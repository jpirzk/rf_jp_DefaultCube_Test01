using System.Collections;
using System.Collections.Generic;
using UnityEngine; 



public class DefaultCubeV2 : MonoBehaviour //jp240125
{   
    // INSPECTOR -----------------------------------------------

    [Header ("AI CONTROL")] 
    [Range(0f, 1f)] public List<float> springForceControl; 

    [Header ("AI INFOS")] 
    public Vector3[] cornerPositions; 
    public Vector3[] cornerVelocities; 
    public Vector3 cubePosition; 

    [Header ("VERSION PARAMETERS")] 
    public float springForceMin = 10f; 
    public float springForceMax = 100f; 
    public float cornerColliderRadius = 0.1f; 
    public float springJointDamper = 0.2f; 
    public float cornerRigidbodyMass = 0.5f;

    [Header ("MONITORING")] 
    public GameObject[] cornerControlObjects; 
    public Rigidbody[] cornerControlObjectRbs; 
    public List<SpringJoint> springJoints; 
    public bool debugSpringForce = false; 
    public bool triggerReset = false; 
    public bool cornerUnderPlane = false; 

    // -------------------------------------------------------

    int[] cornerIds; 
    Vector3[] modifiedVertices; 
    Mesh mesh; 
    Vector3[] defaultVertices; 


    // FUNCTIONS ================================================


    void Start()
    { 
        //remove box collider if necessary 
        if(GetComponent<BoxCollider>()!=null) Destroy(GetComponent<BoxCollider>()); 

        //create control corners from the mesh
        mesh = GetComponent<MeshFilter>().mesh; 
        modifiedVertices = mesh.vertices;  
        CreateCornersFromVertices(mesh.vertices); 
        CreateCornerControlObjects(); 

        //store the default mesh data 
        defaultVertices = mesh.vertices; 

    } 

    void ResetCube(){

        //destroy corners 
        foreach(GameObject _go in cornerControlObjects){
            Destroy(_go);
        } 

        //set mesh to default cube 
        GetComponent<MeshFilter>().mesh.vertices = defaultVertices; 

        //reset all variables 

        //set position.y to 1 
        Vector3 oldPos = this.transform.position; 
        this.transform.position = new Vector3(oldPos.x, 1f, oldPos.z); 

        //create control corners from the mesh
        mesh = GetComponent<MeshFilter>().mesh; 
        modifiedVertices = mesh.vertices;  
        CreateCornersFromVertices(mesh.vertices); 
        CreateCornerControlObjects(); 

    }

    void Update()
    {  
        if(cornerControlObjects.Length > 0){
            //update visible mesh
            modifiedVertices = VerticesFromCornerPoints(); 
            mesh.vertices = modifiedVertices; 
            mesh.RecalculateNormals();
            mesh.RecalculateBounds(); 

            if(triggerReset){ 
                triggerReset = false; 
                ResetCube(); 
            }
        }

        cornerUnderPlane = CheckCornerUnderPlane();
        
    } 

    void FixedUpdate(){

        if(cornerControlObjects.Length > 0){

            //update Cube Position 
            UpdateCubePosition(); 

            //show cube position in public variable 
            cubePosition = transform.position; 

            //update corner component properties
            UpdateSpringProperties(); 
            UpdateRigidbodyProperties(); 

            //update inspector variables with the info about the corners
            UpdateCornerInfo(); 

        }

    } 

    bool CheckCornerUnderPlane(){
        bool isUnderPlane = false; 

        foreach(Vector3 cp in cornerPositions){
            if(cp.y < 0f){
                isUnderPlane = true;
            }
        }

        return isUnderPlane;
    }

    void CreateCornersFromVertices(Vector3[] _verts){
        
        //find vertices with the same value and convert them to a corner (via chatgpt method)

        List<Vector3> _uniqueVectors = new List<Vector3>(); 
        int[] _cornerIds = new int[_verts.Length]; 
        for (int i = 0; i < _verts.Length; i++)
        {
            _verts[i] = transform.TransformPoint(_verts[i]); //convert vectors to world space
            int index = _uniqueVectors.IndexOf(_verts[i]); 
            if (index == -1)
            {
                _uniqueVectors.Add(_verts[i]);
                _cornerIds[i] = _uniqueVectors.Count - 1;
            }
            else
            {
                _cornerIds[i] = index;
            }
        }
        
        cornerPositions = _uniqueVectors.ToArray(); 
        cornerControlObjectRbs = new Rigidbody[_uniqueVectors.Count]; 
        cornerVelocities = new Vector3[_uniqueVectors.Count];
        cornerIds = _cornerIds; 

    } 

    Vector3[] VerticesFromCornerPoints(){
        for(int i=0; i<modifiedVertices.Length; i++){
            modifiedVertices[i] = transform.InverseTransformPoint(cornerPositions[cornerIds[i]]); // convert positions to object space
        } 
        return modifiedVertices; 
    } 

    void UpdateCubePosition(){
        
        //set tranform position to the average corner position
        Vector3 _prevpos = transform.position; 
        transform.position = AveragePositionFromArray(cornerPositions); 
        
        //compensate this transform offset in corners
        foreach(GameObject _cornergo in cornerControlObjects){
            _cornergo.transform.position -= transform.position - _prevpos; 
        }

    }

    void CreateCornerControlObjects(){ 

        //create empties
        cornerControlObjects = new GameObject[cornerPositions.Length]; 

        //put components on
        for(int i=0; i<cornerControlObjects.Length; i++){
            GameObject _go = new GameObject("cornerControl - " + i.ToString()); 
            Rigidbody _rb = _go.AddComponent<Rigidbody>(); 
            _rb.mass = cornerRigidbodyMass; 
            SphereCollider _col = _go.AddComponent<SphereCollider>(); 
            _col.radius = cornerColliderRadius; 
            _go.transform.position = cornerPositions[i]; 
            _go.transform.parent = this.transform; 
            cornerControlObjects[i] = _go; 
            cornerControlObjectRbs[i] = _rb; 
        } 

        //add spring constraints for all of them to all of them 
        springJoints = new List<SpringJoint>(); 
        for(int i=0; i<cornerControlObjects.Length; i++){
            for(int j=0; j<cornerControlObjects.Length; j++){
                if(i<j){
                    SpringJoint _newspring = cornerControlObjects[i].AddComponent<SpringJoint>(); 
                    _newspring.connectedBody = cornerControlObjects[j].GetComponent<Rigidbody>(); 
                    _newspring.spring = springForceMax; //born hard. 
                    _newspring.damper = springJointDamper; 

                    springJoints.Add(_newspring); //store to list 
                    springForceControl.Add(1f); //born hard. 
                }
            } 
        } 
    } 

    void UpdateSpringProperties(){ 
        for(int i=0; i<springJoints.Count; i++){
            //update spring and damper
            springJoints[i].damper = springJointDamper; 
            springJoints[i].spring = Mathf.Lerp(springForceMin, springForceMax, springForceControl[i]); 
            
            //debug spring 
            if(debugSpringForce){ 
                float _minmaxtoggle = (Time.time%2f)>1f ? 1.0f : 0.0f; //toggle min/max every second
                springJoints[i].spring = Mathf.Lerp(springForceMin, springForceMax, _minmaxtoggle); 
            }
        }
    } 

    void UpdateRigidbodyProperties(){
        foreach(GameObject _go in cornerControlObjects){
            Rigidbody _rb = _go.GetComponent<Rigidbody>(); 
            _rb.mass = cornerRigidbodyMass; 
        }
    }

    void UpdateCornerInfo(){
        for(int i=0; i<cornerControlObjects.Length; i++){
            cornerPositions[i] = cornerControlObjects[i].transform.position; 
            cornerVelocities[i] = cornerControlObjectRbs[i].velocity; 
        }
    } 

    Vector3 AveragePositionFromArray(Vector3[] _array){
        Vector3 _averagepos = new Vector3(); 
        foreach(Vector3 _pos in _array){
            _averagepos += _pos;
        }
        _averagepos /= _array.Length; 
        return _averagepos; 
    } 

    void OnApplicationQuit(){
        if(cornerControlObjects.Length > 0){
            foreach(GameObject _go in cornerControlObjects){
                Destroy(_go);
            }
        }
    } 

    

}
