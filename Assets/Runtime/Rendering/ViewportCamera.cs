using UnityEngine;

namespace Runtime.Rendering
{
    [RequireComponent(typeof(Camera))]
    public class ViewportCamera : MonoBehaviour
    {
        private new Camera camera;

        private static ViewportCamera instanceInternal;
        
        public static ViewportCamera instance
        {
            get
            {
                if (instanceInternal == null) instanceInternal = FindFirstObjectByType<ViewportCamera>();
                return instanceInternal;
            }
        }

        public static float fieldOfView
        {
            get => instance.camera.fieldOfView;
            set => instance.camera.fieldOfView = value;
        }
        
        private void Awake()
        {
            camera = GetComponent<Camera>();
        }
    }
}
