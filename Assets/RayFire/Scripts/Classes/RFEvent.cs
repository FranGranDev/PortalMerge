using UnityEngine;
using System.Collections;

// Namespace
namespace RayFire
{
    // Event
    public class RFEvent
    {
        // Delegate & events
        public delegate void EventAction(RayfireRigid rigid);
        public event EventAction LocalEvent;
        
        // Local
        public void InvokeLocalEvent(RayfireRigid rigid)
        {
            if (LocalEvent != null)
                LocalEvent.Invoke(rigid);
        }
    }
    
    // Demolition Event
    public class RFDemolitionEvent : RFEvent
    {
        // Delegate & events
        public static event EventAction GlobalEvent;
        
        // Demolition event
        public static void InvokeGlobalEvent(RayfireRigid rigid)
        {
            if (GlobalEvent != null)
                GlobalEvent.Invoke(rigid);
        }
    }
    
    // Activation Event
    public class RFActivationEvent : RFEvent
    {
        // Delegate & events
        public static event EventAction GlobalEvent;

        // Activation event
        public static void InvokeGlobalEvent(RayfireRigid rigid)
        {
            if (GlobalEvent != null)
                GlobalEvent.Invoke(rigid);
        }
    }
    
    // Restriction Event
    public class RFRestrictionEvent : RFEvent
    {
        // Delegate & events
        public static event EventAction GlobalEvent;

        // Restriction event
        public static void InvokeGlobalEvent(RayfireRigid rigid)
        {
            if (GlobalEvent != null)
                GlobalEvent.Invoke(rigid);
        }
    }
    
    // Shot Event
    public class RFShotEvent
    {
        // Delegate & events
        public delegate void EventAction(RayfireGun gun);
        public static event EventAction GlobalEvent;
        public event EventAction LocalEvent;
       
        // Global
        public static void InvokeGlobalEvent(RayfireGun gun)
        {
            if (GlobalEvent != null)
                GlobalEvent.Invoke(gun);
        }
        
        // Local
        public void InvokeLocalEvent(RayfireGun gun)
        {
            if (LocalEvent != null)
                LocalEvent.Invoke(gun);
        }
    }

    // Explosion Event
    public class RFExplosionEvent
    {
        // Delegate & events
        public delegate void EventAction(RayfireBomb bomb);
        public static event EventAction GlobalEvent;
        public event EventAction LocalEvent;
       
        // Global
        public static void InvokeGlobalEvent(RayfireBomb bomb)
        {
            if (GlobalEvent != null)
                GlobalEvent.Invoke(bomb);
        }
        
        // Local
        public void InvokeLocalEvent(RayfireBomb bomb)
        {
            if (LocalEvent != null)
                LocalEvent.Invoke(bomb);
        }
    }
    
    // Slice Event
    public class RFSliceEvent
    {
        // Delegate & events
        public delegate void EventAction(RayfireBlade blade);
        public static event EventAction GlobalEvent;
        public event EventAction LocalEvent;
       
        // Global
        public static void InvokeGlobalEvent(RayfireBlade blade)
        {
            if (GlobalEvent != null)
                GlobalEvent.Invoke(blade);
        }
        
        // Local
        public void InvokeLocalEvent(RayfireBlade blade)
        {
            if (LocalEvent != null)
                LocalEvent.Invoke(blade);
        }
    }
}