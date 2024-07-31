using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSpawn : MonoBehaviour
{
    public int gridNumber; 
    public float gridSize; 
    public GameObject objectToSpawn; 

    // Start is called before the first frame update
    void Start()
    {
        for(int i=0; i<gridNumber; i++){
            for(int j=0; j<gridNumber; j++){ 
                GameObject _spawnedgo = Instantiate(objectToSpawn, this.transform); 
                Vector3 _offset = (new Vector3(i, 0, j) / gridNumber - new Vector3(0.5f, 0, 0.5f)) * gridSize;
                _spawnedgo.transform.position = transform.position + _offset; 
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
