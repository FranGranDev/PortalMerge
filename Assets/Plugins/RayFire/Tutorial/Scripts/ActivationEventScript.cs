using UnityEngine;

// IMPORTANT! You should use RayFire namespace to use RayFire component's event.
using RayFire;

// Tutorial script. Allows to subscribe to Rigid component activation.
public class ActivationEventScript : MonoBehaviour
{
    // Define if script should subscribe to global activation event
    public bool globalSubscription = false;
    
    // Local Rigid component which will be checked for activation.
    // You can get RayfireRigid component which you want to check for activation in any way you want.
    // This is just a tutorial way to define it.
    public bool localSubscription = false;
    public RayfireRigid localRigidComponent;
    
    // /////////////////////////////////////////////////////////
    // Subscribe/Unsubscribe
    // /////////////////////////////////////////////////////////
    
    // Subscribe to event
    void OnEnable()
    {
        // Subscribe to global activation event. Every activation will invoke subscribed methods. 
        if (globalSubscription == true)
            RFActivationEvent.GlobalEvent += GlobalMethod;
        
        // Subscribe to local activation event. Activation of specific Rigid component will invoke subscribed methods. 
        if (localSubscription == true && localRigidComponent != null)
            localRigidComponent.activationEvent.LocalEvent += LocalMethod;
    }
    
    // Unsubscribe from event
    void OnDisable()
    {
        // Unsubscribe from global activation event.
        if (globalSubscription == true)
            RFActivationEvent.GlobalEvent -= GlobalMethod;
        
        // Unsubscribe from local activation event.
        if (localSubscription == true && localRigidComponent != null)
            localRigidComponent.activationEvent.LocalEvent -= LocalMethod;
    }

    // /////////////////////////////////////////////////////////
    // Subscription Methods
    // /////////////////////////////////////////////////////////
    
    // IMPORTANT!. Subscribed method should has following signature.
    // Void return type and one RayfireRigid input parameter.
    // RayfireRigid input parameter is Rigid component which was activated.
    // In this way you can get activation data.
   
    // Method for local activation subscription
    void LocalMethod(RayfireRigid rigid)
    {
        // Show amount of fragments
        Debug.Log("Local activation: " + rigid.name + " was just activated");
    }
    
    // Method for global activation subscription
    void GlobalMethod(RayfireRigid rigid)
    {
        // Show amount of fragments
        Debug.Log("Global activation: " + rigid.name + " was just activated");
    }
}
