using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class ObjParser
{
    public static void Parse(GameObject obj, string path)
    {
        //atributos
        bool flag = true; //indica si es el primer vertice ya que los minimos están inicializados para no tener errores de compilacion
        float vertminx = 0f, vertminy = 0f, vertminz = 0f;
        float vertmaxx = 0f, vertmaxy = 0f, vertmaxz = 0f;

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
                //si justo esta vacio completo con un -1 e igualmente ya le resto 1 porque los vertices en obj comienzan con 1
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
        
        Debug.Log("Dimensiones: "+ (vertmaxx - vertminx) + "x" + (vertmaxy - vertminy) + "x" + (vertmaxz - vertminz));
        //ENCUENTRO LA DIFERENCIA ENTRE VMIN Y VMAX SOBRE 2 PARA LUEGO RESTAR Y CENTRAR CADA VERTICE
        float restax = (vertminx + vertmaxx) / 2;
        float restay = vertminy;
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
