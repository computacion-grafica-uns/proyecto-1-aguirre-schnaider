using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Globalization;
using System;
using Unity.VisualScripting;




public class SceneManager : MonoBehaviour
{
    
    //objetos agregados
    private GameObject bed;
    private GameObject sofa;
    private string path;

    //objeto camara
    private GameObject miCamara;
    int camaraIndex = 0;

    //Vectores modelMatrix
    private Vector3 newPosition;
    private Vector3 newRotation;
    private Vector3 newScale;
    Matrix4x4 modelMatrix;

    //vectores viewMatrix, cada camara tiene sus propios vectores
    Vector3 fp_pos = new Vector3(4f, 1f, 0f);
    Vector3 fp_forward = new Vector3(-1f, 0f, 0f);
    Vector3 fp_right = new Vector3(0f, 0f, 1f);
    Vector3 orb_pos = new Vector3(5f, 0f, 0f);
    Vector3 orb_target = new Vector3(0f, 0f, 0f);
    Vector3 orb_right = new Vector3(0f, 0f, 1f);
    Matrix4x4 rotY;

    //configuracion de projectionMatrix
    float fov = 90;
    float aspectRatio = 16 / (float)9;
    float nearClipPlane = 0.1f;
    float farClipPlane = 1000;

    void Start()
    {
        InitializeCamera();


        // importo cama a la escena
        path = Application.dataPath + "/Models/bed/bed1.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        bed = new GameObject("bed");
        InicializarObject(bed, path, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f), Color.red);

        //importo sillon a la escena
        path = Application.dataPath + "/Models/sofa/sofa.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        sofa = new GameObject("sofa");
        InicializarObject(sofa, path, new Vector3(3f, 0f, 3f), new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f), Color.red);
    }

    
    void Update()
    {
        if (Input.GetKeyDown("space"))
            camaraIndex = (camaraIndex + 1) % 2;
        else
        {
            if (camaraIndex == 0)
            {
                //CAMARA PRIMERA PERSONA
                //Vectores para desplazamiento, sumo el vector foward y el vector right a la posicion
                fp_forward = fp_forward.normalized;
                fp_right = fp_right.normalized;


                //anulo componentes y para que solo caminen sobre plano xz
                fp_pos = fp_pos + new Vector3(Input.GetAxis("Vertical") * 0.01f * fp_forward.x + Input.GetAxis("Horizontal") * 0.01f * fp_right.x,
                    0,
                    Input.GetAxis("Vertical") * 0.01f * fp_forward.z + Input.GetAxis("Horizontal") * 0.01f * fp_right.z);


                //roto camara, calculo de nuevo los vectores en caso que hayan cambiado por el 
                float inputY = Input.GetAxis("Mouse Y");
                float inputX = Input.GetAxis("Mouse X");

                //roto camara verticalmente
                if ((inputY > 0 && Vector3.Angle(fp_forward, new Vector3(0, 1, 0)) > 25f) ||
                    (inputY < 0 && Vector3.Angle(fp_forward, new Vector3(0, -1, 0)) > 25f))
                {
                    Debug.Log("Rotando camara");
                    Quaternion rotVertical = Quaternion.AngleAxis(-300f * Time.deltaTime * inputY, fp_right);
                    fp_forward = (rotVertical * fp_forward).normalized;
                }

                //roto camara horizontalmente
                if (inputX != 0)
                {
                    Quaternion rotHorizontal = Quaternion.AngleAxis(300f * Time.deltaTime * inputX, new Vector3(0, 1, 0));
                    fp_forward = (rotHorizontal * fp_forward).normalized;
                    fp_right = (rotHorizontal * fp_right).normalized;
                }
            }
            else
            {
                //CAMARA ORBITAL
                //Zoom in o zoom out
                if(Input.GetKey("left shift") && Vector3.Distance(orb_pos, orb_target)>2f )
                {
                    Debug.Log("Zoom in");
                    orb_pos = orb_pos + (orb_target - orb_pos).normalized * 0.01f;
                }
                if (Input.GetKey("left ctrl"))
                {
                    Debug.Log("Zoom out");
                    orb_pos = orb_pos - (orb_target - orb_pos).normalized * 0.01f;
                }


                if (Input.GetAxis("Horizontal") != 0)
                {
                    //Rotacion horizontal
                    orb_right = orb_right.normalized;

                    //matriz con la que roto vectores al rededor del eje y
                    rotY = new Matrix4x4(
                        new Vector4(Mathf.Cos(Mathf.Deg2Rad * Input.GetAxis("Horizontal")), 0f, Mathf.Sin(Mathf.Deg2Rad * Input.GetAxis("Horizontal")), 0f),
                        new Vector4(0f, 1f, 0f, 0f),
                        new Vector4(-Mathf.Sin(Mathf.Deg2Rad * Input.GetAxis("Horizontal")), 0f, Mathf.Cos(Mathf.Deg2Rad * Input.GetAxis("Horizontal")), 0f),
                        new Vector4(0f, 0f, 0f, 1f)
                    );
                    orb_pos = rotY.MultiplyPoint3x4(orb_pos);
                    orb_right = rotY.MultiplyPoint3x4(orb_right);
                }

                if ((Input.GetAxis("Vertical") < 0 && Vector3.Angle((orb_target - orb_pos), new Vector3(0, 1, 0)) > 0.5f) ||
                    (Input.GetAxis("Vertical") > 0 && Vector3.Angle((orb_target - orb_pos), new Vector3(0, -1, 0)) > 0.5f))
                {
                    //Rotacion vertical
                    Quaternion rotVertical = Quaternion.AngleAxis(300f * Time.deltaTime * Input.GetAxis("Vertical"), orb_right);
                    orb_pos = (rotVertical * orb_pos);
                }
            }
            RecalcularMatricesVistaAll();
        }
    }
    
    private void RecalcularMatricesVistaAll()
    {
        RecalcularMatricesVista(bed);
        RecalcularMatricesVista(sofa);

    }
    private void InicializarObject(GameObject obj, string path, Vector3 newPosition, Vector3 newRotation, Vector3 newScale, Color color)
    {
        // Defino posición
        obj.AddComponent<MeshFilter>();
        obj.AddComponent<MeshRenderer>();
        CreateMaterial(obj);
        ObjParser.Parse(obj, path);
        InicializarMatrices(obj, newPosition, newRotation, newScale, color);
    }
    private void AsignarColor(GameObject obj, Color color)
    {
        if (obj != null)
        {
            Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
            int size = mesh.vertices.Length;
            Color[] colors = new Color[size];
            for (int i = 0; i < size; i++)
            {
                colors[i] = color;
            }

            mesh.colors = colors;
        }
    }
    private void CreateMaterial(GameObject obj)
    {
        //creamos un nuevo material que utiliza el shader que le pasemos por parametro
        Material newMaterial = new Material(Shader.Find("ShaderBasico"));
        //asignamos el nuevo material al MeshRenderer
        obj.GetComponent<MeshRenderer>().material = newMaterial;
    }

    private Matrix4x4 CreateModelMatrix(Vector3 newPosition, Vector3 newRotation, Vector3 newScale)
    {
        Matrix4x4 positionMatrix = new Matrix4x4(
            //creo cada columna
            new Vector4(1f, 0f, 0f, newPosition.x),
            new Vector4(0f, 1f, 0f, newPosition.y),
            new Vector4(0f, 0f, 1f, newPosition.z),
            new Vector4(0f, 0f, 0f, 1f)
        );
        positionMatrix = positionMatrix.transpose;

        Matrix4x4 rotationMatrixX = new Matrix4x4(
            new Vector4(1f, 0f, 0f, 0f),
            new Vector4(0f, Mathf.Cos(newRotation.x), -Mathf.Sin(newRotation.x), 0f),
            new Vector4(0f, Mathf.Sin(newRotation.x), Mathf.Cos(newRotation.x), 0f),
            new Vector4(0f, 0f, 0f, 1f)
        );

        Matrix4x4 rotationMatrixY = new Matrix4x4(
            new Vector4(Mathf.Cos(newRotation.y), 0f, Mathf.Sin(newRotation.y), 0f),
            new Vector4(0f, 1f, 0f, 0f),
            new Vector4(-Mathf.Sin(newRotation.y), 0f, Mathf.Cos(newRotation.y), 0f),
            new Vector4(0f, 0f, 0f, 1f)
        );

        Matrix4x4 rotationMatrixZ = new Matrix4x4(
            new Vector4(Mathf.Cos(newRotation.z), -Mathf.Sin(newRotation.z), 0f, 0f),
            new Vector4(Mathf.Sin(newRotation.z), Mathf.Cos(newRotation.z), 0f, 0f),
            new Vector4(0f, 0f, 1f, 0f),
            new Vector4(0f, 0f, 0f, 1f)
        );

        Matrix4x4 rotationMatrix = rotationMatrixZ * rotationMatrixY * rotationMatrixX;
        rotationMatrix = rotationMatrix.transpose;

        Matrix4x4 scaleMatrix = new Matrix4x4(
            new Vector4(newScale.x, 0f, 0f, 0f),
            new Vector4(0f, newScale.y, 0f, 0f),
            new Vector4(0f, 0f, newScale.z, 0f),
            new Vector4(0f, 0f, 0f, 1f)
        );
        scaleMatrix = scaleMatrix.transpose;

        Matrix4x4 finalMatrix = positionMatrix;
        finalMatrix *= rotationMatrix;
        finalMatrix *= scaleMatrix;
        return (finalMatrix);
    }


    private void RecalcularMatricesVista(GameObject obj)
    {

        Matrix4x4 viewMatrix;
        if (camaraIndex == 0)
            viewMatrix = CreateViewMatrix(fp_pos, fp_forward, fp_right);
        else viewMatrix = CreateViewMatrixTarget(orb_pos, orb_target, orb_right);
        obj.GetComponent<Renderer>().material.SetMatrix("_ViewMatrix", viewMatrix);

        Matrix4x4 projectionMatrix = CalculatePerspectiveProjectionMatrix(fov, aspectRatio, nearClipPlane, farClipPlane);
        obj.GetComponent<Renderer>().material.SetMatrix("_ProjectionMatrix", projectionMatrix);
    }
    private void InicializarMatrices(GameObject obj, Vector3 newPosition, Vector3 newRotation, Vector3 newScale, Color color)
    {
        //calculamos la matriz de modelado
        Matrix4x4 modelMatrix = CreateModelMatrix(newPosition, newRotation, newScale);
        //le decimos al shader que utilice esta matriz de modelado
        obj.GetComponent<Renderer>().material.SetMatrix("_ModelMatrix", modelMatrix);
        AsignarColor(obj, color);

        Matrix4x4 viewMatrix;
        if (camaraIndex == 0)
            viewMatrix = CreateViewMatrix(fp_pos, fp_forward, fp_right);
        else viewMatrix = CreateViewMatrixTarget(orb_pos, orb_target, orb_right);
        obj.GetComponent<Renderer>().material.SetMatrix("_ViewMatrix", viewMatrix);

        Matrix4x4 projectionMatrix = CalculatePerspectiveProjectionMatrix(fov, aspectRatio, nearClipPlane, farClipPlane);
        obj.GetComponent<Renderer>().material.SetMatrix("_ProjectionMatrix", projectionMatrix);
    }

    private Matrix4x4 CreateViewMatrix(Vector3 pos, Vector3 forward, Vector3 right)
    {

        forward = forward.normalized;
        right = right.normalized;
        Vector3 up = Vector3.Cross(right, forward).normalized;




        Matrix4x4 viewMatrix = new Matrix4x4(
            new Vector4(right.x, right.y, right.z, -Vector3.Dot(right, pos)),
            new Vector4(up.x, up.y, up.z, -Vector3.Dot(up, pos)),
            new Vector4(-forward.x, -forward.y, -forward.z, Vector3.Dot(forward, pos)),
            new Vector4(0, 0, 0, 1)
        );
        viewMatrix = viewMatrix.transpose;

        return viewMatrix;
    }

    private Matrix4x4 CreateViewMatrixTarget(Vector3 pos, Vector3 target, Vector3 right)
    {

        Vector3 forward = (-pos + target).normalized;
        right = right.normalized;
        Vector3 up = Vector3.Cross(right, forward).normalized;

        Matrix4x4 viewMatrix = new Matrix4x4(
            new Vector4(right.x, right.y, right.z, -Vector3.Dot(right, pos)),
            new Vector4(up.x, up.y, up.z, -Vector3.Dot(up, pos)),
            new Vector4(-forward.x, -forward.y, -forward.z, Vector3.Dot(forward, pos)),
            new Vector4(0, 0, 0, 1)
        );
        viewMatrix = viewMatrix.transpose;

        return viewMatrix;
    }

    private Matrix4x4 CalculatePerspectiveProjectionMatrix(float fov, float aspectRatio, float nearClipPlane, float farClipPlane)
    {

        Matrix4x4 projectionMatrix = new Matrix4x4(
            new Vector4(1 / (aspectRatio * Mathf.Tan(Mathf.Deg2Rad * fov / 2)), 0f, 0f, 0f),
            new Vector4(0f, 1 / Mathf.Tan(Mathf.Deg2Rad * fov / 2), 0f, 0f),
            new Vector4(0f, 0f, (farClipPlane + nearClipPlane) / (nearClipPlane - farClipPlane), 2 * farClipPlane * nearClipPlane / (nearClipPlane - farClipPlane)),
            new Vector4(0f, 0f, -1f, 0f)
        );
        projectionMatrix = projectionMatrix.transpose;

        return projectionMatrix;
    }

    private void InitializeCamera()
    {
        miCamara = new GameObject();
        miCamara.AddComponent<Camera>();
        //el fondo es un color solido                                                                                                                                  
        miCamara.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        //Establecemos el color negro
        miCamara.GetComponent<Camera>().backgroundColor = Color.black;
    }
}

