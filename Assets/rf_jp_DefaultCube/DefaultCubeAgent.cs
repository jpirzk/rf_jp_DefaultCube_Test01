using System.Collections;
using System.Collections.Generic;
using UnityEngine; 

using Unity.MLAgents.Actuators; 
using Unity.MLAgents.Sensors; 
using Unity.MLAgents; 

public class DefaultCubeAgent : Agent
{
    DefaultCubeV2 dcScript; 
    public Transform target; 
    float distPrev; 

    void Start()
    {
        //get default cube script
        dcScript = GetComponent<DefaultCubeV2>(); 
    } 

    public override void OnEpisodeBegin()
    {
        // reset cube if fallen off
        if(this.transform.localPosition.y < -0.5 || dcScript.cornerUnderPlane){
            dcScript.triggerReset = true; 
        }
       
        // move the target to a new spot
        target.localPosition = new Vector3(Random.value*8-4, 0.01f, Random.value*8-4); 

        // store initial distance in distPrev 
        distPrev = Vector3.Distance(target.localPosition, this.transform.localPosition); 
    } 

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions 
        sensor.AddObservation(this.transform.localPosition); 
        sensor.AddObservation(target.localPosition); 
        // corner positions and velocities
        for(int i=0; i<8; i++){
            sensor.AddObservation(dcScript.cornerPositions[i]); 
            sensor.AddObservation(dcScript.cornerVelocities[i]);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // actions, size = 28 
        for(int i=0; i<28; i++){
            dcScript.springForceControl[i] = actionBuffers.ContinuousActions[i]; 
        } 

        // get distance
        float targetDistance = Vector3.Distance(target.localPosition, this.transform.localPosition);
        
        // reached target
        if(targetDistance < 1.42f){
            SetReward(1.0f); 
            EndEpisode(); 
        } 
        else if(this.transform.localPosition.y < -0.5f){
            EndEpisode();
        } 
        else if(dcScript.cornerUnderPlane){
            EndEpisode();
        }

        //tiny rewards on the way 
        float reward = (distPrev-targetDistance) * 0.02f; 
        SetReward(reward); 
        distPrev = targetDistance; 

    } 

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions; 

        bool spacePressed = Input.GetKey(KeyCode.Space); 

        for (int i = 0; i < 28; i++) {
            continuousActionsOut[i] = spacePressed ? 1f : 0f;
        }

    }
}
