using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThiccTapeman.Utils
{
    public static class MouseUtils
    {

        // ---------------------------------------------------- //
        // Getting the mouse world position                     //
        // ---------------------------------------------------- //
        public static Vector3 GetMouseWorldPosition()
        {
            return GetScreenToWorldPosition(UnityEngine.Input.mousePosition, Camera.main, 0);
        }   

        public static Vector3 GetMouseWorldPosition(LayerMask mask)
        {
            return GetScreenToWorldPosition(UnityEngine.Input.mousePosition, Camera.main, mask);
        }

        public static Vector3 GetScreenToWorldPosition(Vector2 screenPos, Camera camera, LayerMask mask)
        {
            if (Physics.Raycast(camera.ScreenPointToRay(screenPos), out RaycastHit hit, 1000f, mask))
            {
                return hit.point;
            }

            return Vector3.zero;
        }
    }
}