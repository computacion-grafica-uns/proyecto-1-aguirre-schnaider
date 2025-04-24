using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Globalization;
using System;




public class FileReader : MonoBehaviour
{
    private float vertminx, vertminy, vertminz;
    private float vertmaxx, vertmaxy, vertmaxz;
    public string objFileName = "paredCorta.obj"; // nombre del archivo dentro de /Assets/Models

    void Start()
    {
        string path = Application.dataPath + "/Models/" + objFileName;

        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }

        GameObject obj = new GameObject("ObjetoImportado");
        obj.AddComponent<MeshFilter>();
        obj.AddComponent<MeshRenderer>();

        Parse(obj, path);

        // Material básico
        Material mat = new Material(Shader.Find("Standard"));
        obj.GetComponent<MeshRenderer>().material = mat;

        // Lo centro en (0,0,0)
        obj.transform.position = Vector3.zero;
    }

    void Parse(GameObject obj, string path)
    {
        StreamReader reader = new StreamReader(path);
        string fileData = reader.ReadToEnd();
        reader.Close();
        //divido el achivo en lineas con el split
        string[] lines = fileData.Split('\n');
        //lista de vertices
        List<Vector3> vertices = new List<Vector3>();
        //listas de salida
        List<int> triangulos = new List<int>();
        List<int> lista_vt = new List<int>();
        List<int> lista_vn = new List<int>();

        // Listas auxiliares para las lineas F
        List<int> vIndices = new List<int>(); // guarda los indices de triangulos
        List<int> vtIndices = new List<int>(); // guarda los indices de textura
        List<int> vnIndices = new List<int>(); //guarda los indices de normales
        bool flag = true;
        for (int k = 0; k < lines.Length; k++)
        {
            if (lines[k].StartsWith("v "))
            {
                //elimino posibles espacios al inicio y separo por los espacios
                // .trim quita espacios al inicio/fin
                string[] tokens = lines[k].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                //Parseo directo de los tres componentes
                float x = float.Parse(tokens[1], CultureInfo.InvariantCulture);
                float y = float.Parse(tokens[2], CultureInfo.InvariantCulture);
                float z = float.Parse(tokens[3], CultureInfo.InvariantCulture);
                // actualizo el bounding-box
                if (flag)
                {
                    flag = false;
                    vertminx = vertmaxx = x;
                    vertminy = vertmaxy = y;
                    vertminz = vertmaxz = z;
                }
                else
                {
                    vertminx = Math.Min(vertminx, x);
                    vertmaxx = Math.Max(vertmaxx, x);
                    vertminy = Math.Min(vertminy, y);
                    vertmaxy = Math.Max(vertmaxy, y);
                    vertminz = Math.Min(vertminz, z);
                    vertmaxz = Math.Max(vertmaxz, z);
                }
                //Almaceno el vertice
                vertices.Add(new Vector3(x, y, z));
            }
            //SI COMIENZA CON F
            else if (lines[k].StartsWith("f "))
            {
                //reinicio las listas auxiliares de v, vt, vn
                vIndices.Clear();
                vtIndices.Clear();
                vnIndices.Clear();
                //Separo por espacios quitando tokens vacios
                string[] tokens = lines[k].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                //Para cada token omito el primero que es "f"
                //si justo esta vacio completo con un -1 e igualmente ya le resto 1 porque los vertices en obj comienzan con 1 (chequear)
                for (int t = 1; t < tokens.Length; t++)
                {
                    string[] parts = tokens[t].Split('/');
                    // v
                    vIndices.Add(int.Parse(parts[0]) - 1);
                    // vt
                    if (parts.Length > 1 && parts[1] != "")
                        vtIndices.Add(int.Parse(parts[1]) - 1);
                    else
                        vtIndices.Add(-1);
                    // vn
                    if (parts.Length > 2)
                        vnIndices.Add(int.Parse(parts[2]) - 1);
                    else
                        vnIndices.Add(-1);
                }
                //Triangulación “fan” para mas de 3 vertices
                for (int i = 1; i < vIndices.Count - 1; i++)
                {
                    // Agrego el triángulo (0, i, i+1)
                    triangulos.Add(vIndices[0]);
                    triangulos.Add(vIndices[i]);
                    triangulos.Add(vIndices[i + 1]);

                    // Si hay texturas, las agrego igual
                    if (lista_vt != null && vtIndices[0] != -1)
                    {
                        lista_vt.Add(vtIndices[0]);
                        lista_vt.Add(vtIndices[i]);
                        lista_vt.Add(vtIndices[i + 1]);
                    }

                    // Si hay normales, idem
                    if (lista_vn != null && vnIndices[0] != -1)
                    {
                        lista_vn.Add(vnIndices[0]);
                        lista_vn.Add(vnIndices[i]);
                        lista_vn.Add(vnIndices[i + 1]);
                    }
                }
            }
        }
        //ENCUENTRO LA DIFERENCIA ENTRE VMIN Y VMAX SOBRE 2 PARA LUEGO RESTAR Y CENTRAR CADA VERTICE
        float restax = (vertminx + vertmaxx) / 2;
        float restay = (vertminy + vertmaxy) / 2;
        float restaz = (vertminz + vertmaxz) / 2;
        for (int j = 0; j < vertices.Count; j++)
        {
            Vector3 v = vertices[j];
            v.x -= restax; //puedo decir v.x entiende que es Coordenadas[0]
            v.y -= restay;
            v.z -= restaz;
            vertices[j] = v;
        }
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangulos.ToArray();
        mesh.RecalculateNormals(); // importante para que se vea bien con iluminación
        mesh.RecalculateBounds();

        obj.GetComponent<MeshFilter>().mesh = mesh;
    }
}
/*
public class SceneManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Parse(GameObject obj, string path)
    {
        //metodo preliminar que acepta lineas de vertices y caras
        //asume obj inicializado y mesh creado


        //leo contenido del archivo
        StreamReader reader = new StreamReader(path);
        string fileData = (reader.ReadToEnd());
        reader.Close();


        //asumo formato correcto del archivo, mesh con triangulos
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangulos = new List<int>();
        
        string[] lines = fileData.Split('\n');
        for (int i = 0; i < lines.Length; i++) {
            if (lines[i].StartsWith("v "))
            {
                //formato de vertice es v x y z
                float[] coordenadas = new float[3]; //guardo como array para poder acceder segun indice
                int indexInicio = 2;       //la primera letra es conocida
                int indexFinal=2;

                //posiciono index en la primera letra de la primer coordenada
                while (lines[i][indexInicio] == ' ')
                    indexInicio++;
                indexFinal = indexInicio;

                //guardo cada coordenada como numero
                for (int j = 0; j < 3; j++) 
                {
                    //indexFinal marca el primer espacio o el primer indice out of bounds
                    while (indexFinal<lines[i].Length && lines[i][indexFinal]!=' ' && lines[i][indexFinal] != '\n' )
                        indexFinal++;

                    coordenadas[j] = float.Parse(lines[i].Substring(indexInicio, indexFinal - indexInicio ), CultureInfo.InvariantCulture);

                    //salteo espacios hasta la proxima letra
                    while(lines[i][indexFinal] == ' ')
                        indexFinal++;
                    indexInicio = indexFinal;
                }

                vertices.Add(new Vector3(coordenadas[0], coordenadas[1], coordenadas[2]));
            }else if(lines[i].StartsWith("f "))
            {
                //formato de cara es f c1 c2 c3
                int[] aristas= new int[3]; //guardo como array para poder acceder segun indice
                int indexInicio = 2;       //la primera letra es conocida
                int indexFinal = 2;

                //posiciono index en la primera letra de la primera arista
                while (lines[i][indexInicio] == ' ')
                    indexInicio++;
                indexFinal = indexInicio;

                //guardo cada coordenada como numero
                for (int j = 0; j < 3; j++)
                {
                    //indexFinal marca el primer espacio o el primer indice out of bounds
                    while (indexFinal < lines[i].Length && lines[i][indexFinal] != ' ' && lines[i][indexFinal] != '\n')
                        indexFinal++;

                    aristas[j] = int.Parse(lines[i].Substring(indexInicio, indexFinal - indexInicio)) -1;

                    //salteo espacios hasta la proxima letra
                    while (indexFinal < lines[i].Length && lines[i][indexFinal] == ' ')
                        indexFinal++;
                    indexInicio = indexFinal;
                }

                //agrego los indices de las aristas a la lista de triangulos
                triangulos.Add(aristas[0]);
                triangulos.Add(aristas[1]);
                triangulos.Add(aristas[2]);
            }
        }
           
        obj.GetComponent<MeshFilter>().mesh.vertices = vertices.ToArray();
        obj.GetComponent<MeshFilter>().mesh.triangles = triangulos.ToArray();   

    }
}*/
