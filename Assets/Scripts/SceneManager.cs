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
    private GameObject bacha;
    private GameObject horno;
    private GameObject gabineteCocina1;
    private GameObject mesa;
    private GameObject silla1;
    private GameObject silla2;
    private GameObject silla3;
    private GameObject silla4;
    private GameObject armario;
    private GameObject estante;
    private GameObject heladera;
    private GameObject ducha;
    private GameObject inodoro;
    private GameObject lavamanos;
    private GameObject calavera;
    private GameObject sombrero;
    private GameObject lentes;
    private GameObject planta1;
    private GameObject planta2;
    private GameObject pared1;
    private GameObject pared2;
    private GameObject pared3;
    private GameObject pared4;
    private GameObject paredBano;
    private GameObject piso;
    private GameObject techo;

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
    Vector3 fp_pos = new Vector3(0f, 1.5f, 0f);
    Vector3 fp_forward = new Vector3(1f, 0f, 0f);
    Vector3 fp_right = new Vector3(0f, 0f, -1f);
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

        //importo objetos a la escena
        AcomodarParedes();
        AcomodarLiving();
        AcomodarCocina();
        AcomodarBano();
        AcomodarCalavera();



    }




    void Update()
    {
        if (Input.GetKeyDown("space"))
            camaraIndex = (camaraIndex + 1) % 2;
        else if (Input.GetKeyDown("v"))
            AlternarVisibilidadParedes();
        else if (Input.GetKeyDown("r"))
            AlternarVisibilidadTecho();
        else if (Input.GetKeyDown("f"))
            AlternarVisibilidadPiso();
        else
        {
            if (camaraIndex == 0)
            {
                //CAMARA PRIMERA PERSONA
                //Vectores para desplazamiento, sumo el vector foward y el vector right a la posicion
                fp_forward = fp_forward.normalized;
                fp_right = fp_right.normalized;


                //anulo componentes y para que solo caminen sobre plano xz
                fp_pos = fp_pos + new Vector3(Input.GetAxis("Vertical") * 3f * Time.deltaTime * fp_forward.x + Input.GetAxis("Horizontal") * 3f * Time.deltaTime * fp_right.x,
                    0,
                    Input.GetAxis("Vertical") * 3f * Time.deltaTime * fp_forward.z + Input.GetAxis("Horizontal") * 3f * Time.deltaTime * fp_right.z);


                //roto camara, calculo de nuevo los vectores en caso que hayan cambiado por el 
                float inputY = Input.GetAxis("Mouse Y");
                float inputX = Input.GetAxis("Mouse X");

                //roto camara verticalmente
                if ((inputY > 0 && Vector3.Angle(fp_forward, new Vector3(0, 1, 0)) > 25f) ||
                    (inputY < 0 && Vector3.Angle(fp_forward, new Vector3(0, -1, 0)) > 25f))
                {
                    Quaternion rotVertical = Quaternion.AngleAxis(-100f * Time.deltaTime * inputY, fp_right);
                    fp_forward = (rotVertical * fp_forward).normalized;
                }

                //roto camara horizontalmente
                if (inputX != 0)
                {
                    Quaternion rotHorizontal = Quaternion.AngleAxis(100f * Time.deltaTime * inputX, new Vector3(0, 1, 0));
                    fp_forward = (rotHorizontal * fp_forward).normalized;
                    fp_right = (rotHorizontal * fp_right).normalized;
                }
            }
            else
            {
                //CAMARA ORBITAL
                //Zoom in o zoom out
                if (Input.GetKey("left shift") && Vector3.Distance(orb_pos, orb_target) > 2f)
                {
                    orb_pos = orb_pos + (orb_target - orb_pos).normalized * 5f * Time.deltaTime;
                }
                if (Input.GetKey("left ctrl"))
                {
                    orb_pos = orb_pos - (orb_target - orb_pos).normalized * 5f * Time.deltaTime;
                }


                if (Input.GetAxis("Horizontal") != 0)
                {
                    //Rotacion horizontal
                    orb_right = orb_right.normalized;

                    //matriz con la que roto vectores al rededor del eje y
                    rotY = new Matrix4x4(
                        new Vector4(Mathf.Cos(Mathf.Deg2Rad * Input.GetAxis("Horizontal") * Time.deltaTime * 100), 0f, Mathf.Sin(Mathf.Deg2Rad * Input.GetAxis("Horizontal") * Time.deltaTime * 100), 0f),
                        new Vector4(0f, 1f, 0f, 0f),
                        new Vector4(-Mathf.Sin(Mathf.Deg2Rad * Input.GetAxis("Horizontal") * Time.deltaTime * 100), 0f, Mathf.Cos(Mathf.Deg2Rad * Input.GetAxis("Horizontal") * Time.deltaTime * 100), 0f),
                        new Vector4(0f, 0f, 0f, 1f)
                    );
                    orb_pos = rotY.MultiplyPoint3x4(orb_pos);
                    orb_right = rotY.MultiplyPoint3x4(orb_right);
                }

                if ((Input.GetAxis("Vertical") < 0 && Vector3.Angle((orb_target - orb_pos), new Vector3(0, 1, 0)) > 0.5f) ||
                    (Input.GetAxis("Vertical") > 0 && Vector3.Angle((orb_target - orb_pos), new Vector3(0, -1, 0)) > 0.5f))
                {
                    //Rotacion vertical
                    Quaternion rotVertical = Quaternion.AngleAxis(100f * Time.deltaTime * Input.GetAxis("Vertical"), orb_right);
                    orb_pos = (rotVertical * orb_pos);
                }
            }
            RecalcularMatricesVistaAll();
        }
    }

    private void AcomodarParedes()
    {
        //importo las paredes

        //PARED 1
        path = Application.dataPath + "/Models/Paredes/pared1_y_3.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        pared1 = new GameObject("pared1_y_3");
        InicializarObject(pared1, path, new Vector3(0f, 0f, -6f), new Vector3(0f, Mathf.Deg2Rad * 90f, 0f), new Vector3(1f, 1f, 1f), new Color(0.82f, 0.74f, 0.63f));

        //PARED 2
        path = Application.dataPath + "/Models/Paredes/pared2.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        pared2 = new GameObject("pared2");
        InicializarObject(pared2, path, new Vector3(-3f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f), new Color(0.82f, 0.74f, 0.63f));

        //PARED 3
        path = Application.dataPath + "/Models/Paredes/pared1_y_3.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        pared3 = new GameObject("pared3");
        InicializarObject(pared3, path, new Vector3(0f, 0f, 6f), new Vector3(0f, Mathf.Deg2Rad * 90f, 0f), new Vector3(1f, 1f, 1f), new Color(0.82f, 0.74f, 0.63f));

        //PARED 4
        path = Application.dataPath + "/Models/Paredes/pared4.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        pared4 = new GameObject("pared4");
        InicializarObject(pared4, path, new Vector3(3f, 3f, 0f), new Vector3(0f, Mathf.Deg2Rad * 180f, 0f), new Vector3(1f, -1f, 1f), new Color(0.82f, 0.74f, 0.63f));

        //importo pared baño
        path = Application.dataPath + "/Models/Paredes/paredBaño.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        paredBano = new GameObject("paredBaño");
        InicializarObject(paredBano, path, new Vector3(0f, 3f, 4f), new Vector3(0f, Mathf.Deg2Rad * 90f, 0f), new Vector3(1f, -1f, 1f), new Color(0.82f, 0.74f, 0.63f));

        //importo piso
        path = Application.dataPath + "/Models/Paredes/piso_techo.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        piso = new GameObject("piso");
        InicializarObject(piso, path, new Vector3(0f, -0.01f, 0f), new Vector3(0f, Mathf.Deg2Rad * 90f, 0f), new Vector3(1f, 1f, 1f), new Color(0.4f, 0.4f, 0.4f));

        //importo techo
        path = Application.dataPath + "/Models/Paredes/piso_techo.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        techo = new GameObject("techo");
        InicializarObject(techo, path, new Vector3(0f, 3.01f, 0f), new Vector3(Mathf.Deg2Rad * 180f , Mathf.Deg2Rad * 90f, 0f), new Vector3(1f, 1f, 1f), new Color(0.7875f, 0.7106f, 0.6051f));
    }

    private void AcomodarLiving()
    {
        // importo cama a la escena
        path = Application.dataPath + "/Models/bed/bed1.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        bed = new GameObject("bed");
        InicializarObject(bed, path, new Vector3(-1.75f, 0f, 1.8f), new Vector3(0f, -Mathf.Deg2Rad * 90, 0f), new Vector3(0.85f, 0.85f, 0.85f), new Color(0.55f, 0.3f, 0.2f));


        //importo sillon a la escena
        path = Application.dataPath + "/Models/sofa/sofa.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        sofa = new GameObject("sofa");
        InicializarObject(sofa, path, new Vector3(-2.37f, 0f, -0.75f), new Vector3(0f, 0f, 0f), new Vector3(0.75f, 0.75f, 0.75f), new Color(0.3f, 0.18f, 0.12f));


        //importo mesa
        path = Application.dataPath + "/Models/table/table.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        mesa = new GameObject("mesa");
        InicializarObject(mesa, path, new Vector3(0f, 0f, -2.5f), new Vector3(0f, 0f, 0f), new Vector3(0.75f, 0.75f, 0.75f), new Color(0.4f, 0.25f, 0.1f));

        //importo silla1
        path = Application.dataPath + "/Models/chair1/chair1.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        silla1 = new GameObject("silla1");
        InicializarObject(silla1, path, new Vector3(0.9f, 0f, -2.5f), new Vector3(0f, Mathf.Deg2Rad * 180, 0f), new Vector3(0.751f, 0.75f, 0.75f), new Color(0.45f, 0.3f, 0.15f));

        //importo silla2
        path = Application.dataPath + "/Models/chair1/chair1.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        silla2 = new GameObject("silla2");
        InicializarObject(silla2, path, new Vector3(-0.9f, 0f, -2.5f), new Vector3(0f, 0f, 0f), new Vector3(0.751f, 0.75f, 0.75f), new Color(0.45f, 0.3f, 0.15f));

        //importo silla3
        path = Application.dataPath + "/Models/chair1/chair1.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        silla3 = new GameObject("silla3");
        InicializarObject(silla3, path, new Vector3(0f, 0f, -1.6f), new Vector3(0f, Mathf.Deg2Rad * 90, 0f), new Vector3(0.751f, 0.75f, 0.75f), new Color(0.45f, 0.3f, 0.15f));

        //importo silla4
        path = Application.dataPath + "/Models/chair1/chair1.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        silla4 = new GameObject("silla4");
        InicializarObject(silla4, path, new Vector3(0f, 0f, -3.4f), new Vector3(0f, -Mathf.Deg2Rad * 90, 0f), new Vector3(0.751f, 0.75f, 0.75f), new Color(0.45f, 0.3f, 0.15f));

        //importo armario
        path = Application.dataPath + "/Models/Wardrobe1/Wardrobe1.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        armario = new GameObject("armario");
        InicializarObject(armario, path, new Vector3(2.5f, 0f, 2.5f), new Vector3(0f, Mathf.Deg2Rad * 180, 0f), new Vector3(1f, 1f, 1f), new Color(0.42f, 0.26f, 0.14f));

        //importo estante
        path = Application.dataPath + "/Models/Wardrobe2/Wardrobe2.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        estante = new GameObject("estante");
        InicializarObject(estante, path, new Vector3(-1f, 0f, 3.5f), new Vector3(0f, Mathf.Deg2Rad * 90, 0f), new Vector3(1f, 1f, 1f), new Color(0.42f, 0.26f, 0.14f));

        //importo plantas
        path = Application.dataPath + "/Models/plant/plant.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        planta1 = new GameObject("planta1");
        InicializarObject(planta1, path, new Vector3(2.5f, 0f, -1.7f), new Vector3(0f, Mathf.Deg2Rad * 180, 0f), new Vector3(0.014f, 0.03f, 0.014f), new Color(0.25f, 0.5f, 0.2f));

        //importo plantas
        path = Application.dataPath + "/Models/plant/plant.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        planta2 = new GameObject("planta2");
        InicializarObject(planta2, path, new Vector3(-2.5f, 0f, -2.50f), new Vector3(0f, Mathf.Deg2Rad * 180, 0f), new Vector3(0.014f, 0.03f, 0.014f), new Color(0.25f, 0.5f, 0.2f));

    }
    private void AcomodarCocina()
    {
        //importo bacha cocina
        path = Application.dataPath + "/Models/KitchenCabinetRounded/KitchenCabinetRounded.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        bacha = new GameObject("bacha");
        InicializarObject(bacha, path, new Vector3(-1.73f, 0f, -4.69f), new Vector3(0f, -Mathf.Deg2Rad * 90, 0f), new Vector3(1f, 1f, 1f), new Color(0.45f, 0.28f, 0.15f));

        //importo horno
        path = Application.dataPath + "/Models/KitchenStoveWithOven/KitchenStoveWithOven.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        horno = new GameObject("horno");
        InicializarObject(horno, path, new Vector3(-0.10f, 0f, -5.38f), new Vector3(0f, -Mathf.Deg2Rad * 90, 0f), new Vector3(1f, 1f, 1f), new Color(0.25f, 0.25f, 0.25f));

        //importo gabinete cocina
        path = Application.dataPath + "/Models/KitchenCabinet1/KitchenCabinet1.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        gabineteCocina1 = new GameObject("gabineteCocina");
        InicializarObject(gabineteCocina1, path, new Vector3(0.88f, 0f, -5.38f), new Vector3(0f, -Mathf.Deg2Rad * 90, 0f), new Vector3(1f, 1f, 1f), new Color(0.55f, 0.38f, 0.2f));

        //importo heladera
        path = Application.dataPath + "/Models/Fridge/Fridge.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        heladera = new GameObject("heladera");
        InicializarObject(heladera, path, new Vector3(1.79f, 0f, -5.39f), new Vector3(0f, -Mathf.Deg2Rad * 90, 0f), new Vector3(1f, 1f, 1f), new Color(0.75f, 0.75f, 0.78f));
    }

    private void AcomodarBano()
    {

        //importo ducha
        path = Application.dataPath + "/Models/shower/shower.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        ducha = new GameObject("ducha");
        InicializarObject(ducha, path, new Vector3(-2.45f, 0f, 5.39f), new Vector3(0f, -Mathf.Deg2Rad * 0f, 0f), new Vector3(1f, 1f, 1f), new Color(0.55f, 0.7f, 0.85f));

        //importo inodoro
        path = Application.dataPath + "/Models/toilet2/toilet2.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        inodoro = new GameObject("inodoro");
        InicializarObject(inodoro, path, new Vector3(0f, 0f, 5.57f), new Vector3(0f, Mathf.Deg2Rad * 90f, 0f), new Vector3(0.6f, 0.6f, 0.6f), new Color(0.9f, 0.92f, 0.95f));

        //importo lavamanos
        path = Application.dataPath + "/Models/sink/sink.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        lavamanos = new GameObject("lavamanos");
        InicializarObject(lavamanos, path, new Vector3(2f, 0f, 5.61f), new Vector3(0f, Mathf.Deg2Rad * 90f, 0f), new Vector3(0.9f, 0.9f, 0.9f), new Color(0.9f, 0.92f, 0.95f));
    }

    private void AcomodarCalavera()
    {
        //objeto jerarquico por lo que es tratado diferente
        path = Application.dataPath + "/Models/objetoJerarquico/skull.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        calavera = new GameObject("calavera");
        Matrix4x4 modelMatrixCalavera = InicializarObject(calavera, path, new Vector3(-1f, 1.37f, 3.49f), new Vector3(0f, Mathf.Deg2Rad * 180, 0f), new Vector3(0.05f, 0.05f, 0.05f), new Color(1f, 1f, 1f));

        //defino la matriz de modelado del sombrero en base a la matriz de modelado de la calavera
        path = Application.dataPath + "/Models/objetoJerarquico/hat.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        sombrero = new GameObject("sombrero");
        sombrero.AddComponent<MeshFilter>();
        sombrero.AddComponent<MeshRenderer>();
        CreateMaterial(sombrero);
        ObjParser.Parse(sombrero, path);
        //creo la modelMatrix a partir de la modelMatrix de la calavera
        Matrix4x4 modelMatrixSombrero = modelMatrixCalavera * CreateModelMatrix(new Vector3(-3.2f,6f, 3.2f), new Vector3(-Mathf.Deg2Rad * 90, 0f ,-Mathf.Deg2Rad * 45), new Vector3(2.7f, 2.7f, 2.7f));
        sombrero.GetComponent<Renderer>().material.SetMatrix("_ModelMatrix", modelMatrixSombrero);
        AsignarColor(sombrero, new Color(0f,0f,0f));
        RecalcularMatricesVista(sombrero);
        
        
        //defino la matriz de modelado del sombrero en base a la matriz de modelado de la calavera
        path = Application.dataPath + "/Models/objetoJerarquico/sunglasses1.obj";
        if (!File.Exists(path))
        {
            Debug.LogError("Archivo .obj no encontrado en: " + path);
            return;
        }
        lentes = new GameObject("lentes");
        lentes.AddComponent<MeshFilter>();
        lentes.AddComponent<MeshRenderer>();
        CreateMaterial(lentes);
        ObjParser.Parse(lentes, path);
        //creo la modelMatrix a partir de la modelMatrix de la calavera
        Matrix4x4 modelMatrixLentes = modelMatrixCalavera * CreateModelMatrix(new Vector3(0f, 3f, 1.1f), new Vector3(0, 0f, 0), new Vector3(3.6f, 3f, 3f));
        lentes.GetComponent<Renderer>().material.SetMatrix("_ModelMatrix", modelMatrixLentes);
        AsignarColor(lentes, new Color(0f, 0f, 0f));
        RecalcularMatricesVista(lentes);
        
    }

    private void AlternarVisibilidadParedes()
    {
        pared1.GetComponent<Renderer>().enabled = !pared1.GetComponent<Renderer>().enabled;
        pared2.GetComponent<Renderer>().enabled = !pared2.GetComponent<Renderer>().enabled;
        pared3.GetComponent<Renderer>().enabled = !pared3.GetComponent<Renderer>().enabled;
        pared4.GetComponent<Renderer>().enabled = !pared4.GetComponent<Renderer>().enabled;
    }

    private void AlternarVisibilidadTecho()
    {
        techo.GetComponent<Renderer>().enabled = !techo.GetComponent<Renderer>().enabled;
    }

    private void AlternarVisibilidadPiso()
    {
        piso.GetComponent<Renderer>().enabled = !piso.GetComponent<Renderer>().enabled;
    }

    private void RecalcularMatricesVistaAll()
    {
        RecalcularMatricesVista(bed);
        RecalcularMatricesVista(sofa);
        RecalcularMatricesVista(bacha);
        RecalcularMatricesVista(horno);
        RecalcularMatricesVista(gabineteCocina1);
        RecalcularMatricesVista(heladera);
        RecalcularMatricesVista(mesa);
        RecalcularMatricesVista(silla1);
        RecalcularMatricesVista(silla2);
        RecalcularMatricesVista(silla3);
        RecalcularMatricesVista(silla4);
        RecalcularMatricesVista(armario);
        RecalcularMatricesVista(estante);
        RecalcularMatricesVista(ducha);
        RecalcularMatricesVista(inodoro);
        RecalcularMatricesVista(lavamanos);
        RecalcularMatricesVista(calavera);
        RecalcularMatricesVista(sombrero);
        RecalcularMatricesVista(lentes);
        RecalcularMatricesVista(planta1);
        RecalcularMatricesVista(planta2);
        RecalcularMatricesVista(pared1);
        RecalcularMatricesVista(pared2);
        RecalcularMatricesVista(pared3);
        RecalcularMatricesVista(pared4);
        RecalcularMatricesVista(paredBano);
        RecalcularMatricesVista(piso);
        RecalcularMatricesVista(techo);



    }
    private Matrix4x4 InicializarObject(GameObject obj, string path, Vector3 newPosition, Vector3 newRotation, Vector3 newScale, Color color)
    {
        // Defino posición
        obj.AddComponent<MeshFilter>();
        obj.AddComponent<MeshRenderer>();
        CreateMaterial(obj);
        ObjParser.Parse(obj, path);
        return InicializarMatrices(obj, newPosition, newRotation, newScale, color);
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
    private Matrix4x4 InicializarMatrices(GameObject obj, Vector3 newPosition, Vector3 newRotation, Vector3 newScale, Color color)
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

        return modelMatrix;
    }

    private Matrix4x4 CreateViewMatrix(Vector3 pos, Vector3 forward, Vector3 right)
    {

        forward = forward.normalized;
        right = right.normalized;
        Vector3 up = -Vector3.Cross(right, forward).normalized;




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
        Vector3 up = -Vector3.Cross(right, forward).normalized;

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

