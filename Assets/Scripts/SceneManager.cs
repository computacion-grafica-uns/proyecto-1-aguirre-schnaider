using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Globalization;
using System;

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
}
