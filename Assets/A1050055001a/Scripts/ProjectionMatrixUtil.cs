using UnityEngine;
using System.Collections;

public static class ProjectionMatrixUtil
{
    private static float[] _intrinsics;

    // Use this for initialization
    static ProjectionMatrixUtil()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.IPhonePlayer:
            /* Define intrinsic parameters: iPhone */
                float[] iPhoneIntrinsics = {
                617.750f,    0.0f,        317.206f,
                0.0f,        618.238f,    244.322f,
                0.0f,        0.0f,        1.0f};

                /*
                 * Use the following one for iPad

                float[] iPadIntrinsics = {
                785.392f,    0.0f,        318.726f,
                0.0f,        783.778f,    225.411f,
                0.0f,        0.0f,        1.0f};
                */

                _intrinsics = iPhoneIntrinsics;
                break;
            case RuntimePlatform.Android:
            /* Define intrinsic parameters: Android*/
                float[] androidIntrinsics = {
                785.39254f,  0.0f,        318.72601f,
                0.0f,        783.77783f,  225.41132f,
                0.0f,        0.0f,        1.0f};

                _intrinsics = androidIntrinsics;
                break;
            default:
                float[] windowsIntrinsics = {
            785.39254f,  0.0f,        318.72601f,
            0.0f,        783.77783f,  225.41132f,
            0.0f,        0.0f,        1.0f};

                _intrinsics = windowsIntrinsics;
                break;
        }
    }

    public static void UpdateProjectionMatrix(Camera camera, int cameraWidth, int cameraHeight, Vector2 screenCameraRatio)
    {
        Matrix4x4 projectionMatrix;

        int screenOrientation;
        if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android)
        {
            screenOrientation = (int)Screen.orientation;
        } else
        {
            screenOrientation = (int)ScreenOrientation.LandscapeLeft; 
        }

        projectionMatrix = GetProjectionMatrix(screenOrientation, camera.nearClipPlane, camera.farClipPlane, cameraWidth, cameraHeight, screenCameraRatio.x, screenCameraRatio.y);


        if (projectionMatrix != Matrix4x4.zero)
        {
            camera.projectionMatrix = projectionMatrix;
        }
    }

    public static Matrix4x4 GetProjectionMatrix(int screenOrientation, float nearPlane, float farPlane, int cameraWidth, int cameraHeight, float onScreenCameraWidthRatio, float onScreenCameraHeightRatio)
    {
        float px, py, u0, v0, width, height;
        if (cameraWidth < cameraHeight)
        {
            if (screenOrientation == 1 || screenOrientation == 2)
            {
                px = _intrinsics [0];
                py = _intrinsics [4];
                if (screenOrientation == 1)
                {
                    u0 = _intrinsics [2] * (1.0f - (1.0f - onScreenCameraHeightRatio));
                    v0 = _intrinsics [5] * (1.0f - (1.0f - onScreenCameraWidthRatio));
                } else
                {
                    u0 = (cameraWidth - _intrinsics [2]) * (1.0f - (1.0f - onScreenCameraHeightRatio));
                    v0 = (cameraHeight - _intrinsics [5]) * (1.0f - (1.0f - onScreenCameraWidthRatio));
                }
                width = cameraWidth * onScreenCameraHeightRatio;
                height = cameraHeight * onScreenCameraWidthRatio;
            } else
            {
                px = _intrinsics [4];
                py = _intrinsics [0];
                if (screenOrientation == 3)
                {
                    u0 = _intrinsics [5] * (1.0f - (1.0f - onScreenCameraHeightRatio));
                    v0 = _intrinsics [2] * (1.0f - (1.0f - onScreenCameraWidthRatio));
                } else
                {
                    u0 = (cameraHeight - _intrinsics [5]) * (1.0f - (1.0f - onScreenCameraHeightRatio));
                    v0 = (cameraWidth - _intrinsics [2]) * (1.0f - (1.0f - onScreenCameraWidthRatio));
                }
                width = cameraHeight * onScreenCameraHeightRatio;
                height = cameraWidth * onScreenCameraWidthRatio;
            }
        } else
        {
            if (screenOrientation == 1 || screenOrientation == 2)
            {
                px = _intrinsics [4];
                py = _intrinsics [0];
                if (screenOrientation == 1)
                {
                    u0 = _intrinsics [5] * (1.0f - (1.0f - onScreenCameraWidthRatio));
                    v0 = _intrinsics [2] * (1.0f - (1.0f - onScreenCameraHeightRatio));
                } else
                {
                    u0 = (cameraHeight - _intrinsics [5]) * (1.0f - (1.0f - onScreenCameraWidthRatio));
                    v0 = (cameraWidth - _intrinsics [2]) * (1.0f - (1.0f - onScreenCameraHeightRatio));
                }
                width = cameraHeight * onScreenCameraWidthRatio;
                height = cameraWidth * onScreenCameraHeightRatio;
            } else
            {
                px = _intrinsics [0];
                py = _intrinsics [4];
                if (screenOrientation == 3)
                {
                    u0 = (cameraWidth - _intrinsics [2]) * (1.0f - (1.0f - onScreenCameraWidthRatio));
                    v0 = _intrinsics [5] * (1.0f - (1.0f - onScreenCameraHeightRatio));
                } else
                {
                    u0 = _intrinsics [2] * (1.0f - (1.0f - onScreenCameraWidthRatio));
                    v0 = (cameraHeight - _intrinsics [5]) * (1.0f - (1.0f - onScreenCameraHeightRatio));
                }
                width = cameraWidth * onScreenCameraWidthRatio;
                height = cameraHeight * onScreenCameraHeightRatio;
            }
        }

        Matrix4x4 projMatrix = Matrix4x4.zero;
        /*see http://www.songho.ca/opengl/gl_projectionmatrix.html for explanations on matrix computation*/
        projMatrix [0] = 2.0f * px / width;
        projMatrix [4] = 0.0f;
        projMatrix [8] = 2.0f * (u0 / width) - 1.0f;
        projMatrix [12] = 0.0f;
        projMatrix [1] = 0.0f;
        projMatrix [5] = 2.0f * py / height;
        projMatrix [9] = 2.0f * (v0 / height) - 1.0f;
        projMatrix [13] = 0.0f;
        projMatrix [2] = 0.0f;
        projMatrix [6] = 0.0f;
        projMatrix [10] = - (farPlane + nearPlane) / (farPlane - nearPlane);
        projMatrix [14] = - (2.0f * farPlane * nearPlane) / (farPlane - nearPlane);
        projMatrix [3] = 0.0f;
        projMatrix [7] = 0.0f;
        projMatrix [11] = -1.0f;
        projMatrix [15] = 0.0f;

        return projMatrix;
    }
}
