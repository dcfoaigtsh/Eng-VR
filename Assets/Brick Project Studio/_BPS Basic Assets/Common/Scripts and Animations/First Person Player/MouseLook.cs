using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SojaExiles
{
    public class MouseLook : MonoBehaviour
    {
        public float mouseXSensitivity = 100f;
        public Transform playerBody;

        float xRotation = 0f;
        public bool canLook = true;  // 可以控制視角的開關

        void Start()
        {
            // ⭐ 不再一開始就鎖滑鼠！
            // 預設讓玩家能看到滑鼠，直到自己決定要鎖
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (!canLook)
                return;

            float mouseX = Input.GetAxis("Mouse X") * mouseXSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseXSensitivity * Time.deltaTime;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            playerBody.Rotate(Vector3.up * mouseX);
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            canLook = true;
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            canLook = false;
        }
    }
}
