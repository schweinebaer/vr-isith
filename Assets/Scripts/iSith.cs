using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class iSith : MonoBehaviour
{
    private GameObject scene = null;

    private GameObject leftHandController;

    private LineRenderer leftRayRenderer;

    private GameObject rightHandController;

    private LineRenderer rightRayRenderer;

    private Vector3 leftRayClosestPointV1;

    private Vector3 rightRayClosestPointV1;

    private Vector3 leftRayClosestPointV2;

    private Vector3 rightRayClosestPointV2;

    private GameObject forceV1;

    private GameObject forceV2;

    private GameObject forceThreshold;

    private GameObject selectedObject = null;

    private CollisionDetector collisionDetector;

    private Color32 standardColor = new Color32(255, 255, 255, 255);

    private Color32 forceHighlightColor = new Color32(230, 0, 0, 255);

    private XRController rightXRController;

    private List<UnityEngine.XR.InputDevice> inputDevices;

    private UnityEngine.XR.InputDevice device;

    private GameObject rotationSphere;

    private bool rotationMode = false;

    private Color32 rotationColor = new Color32(0, 230, 0, 255);

    public LineRenderer rotationLineRenderer;

    void Start()
    {
        Debug.Log("iSith started");
        scene = GameObject.Find("Scene");
        leftHandController = GameObject.Find("LeftHand Controller");
        leftRayRenderer = leftHandController.GetComponent<LineRenderer>();
        rightHandController = GameObject.Find("RightHand Controller");
        rightXRController = rightHandController.GetComponent<XRController>();
        rightRayRenderer = rightHandController.GetComponent<LineRenderer>();
        forceV1 = GameObject.Find("ForceV1");
        forceV2 = GameObject.Find("ForceV2");
        forceThreshold = GameObject.Find("ForceThreshold");
        collisionDetector = forceV2.GetComponent<CollisionDetector>();
        rotationSphere = GameObject.Find("RotationSphere");
        rotationLineRenderer =
            new GameObject("Line").AddComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        forceV1.SetActive(false);
        forceV2.SetActive(false);
        forceThreshold.SetActive(false);
        rotationSphere.SetActive(false);

        // register Right Hand
        if (device.role.ToString() != "RightHanded")
        {
            var rightHandDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine
                .XR
                .InputDevices
                .GetDevicesAtXRNode(UnityEngine.XR.XRNode.RightHand,
                rightHandDevices);
            if (rightHandDevices.Count == 1)
            {
                device = rightHandDevices[0];
                Debug
                    .Log(string
                        .Format("Device name '{0}' with role '{1}'",
                        device.name,
                        device.role.ToString()));
            }
        }

        // check if rays are up.
        if (
            leftRayRenderer.positionCount == 2 &&
            rightRayRenderer.positionCount == 2
        )
        {
            // get ray data
            Vector3 leftRayStart = leftRayRenderer.transform.position;
            Vector3 leftRayEnd = leftRayRenderer.GetPosition(1);
            Vector3 leftRayDirection = leftRayStart - leftRayEnd;
            Vector3 rightRayStart = rightRayRenderer.transform.position;
            Vector3 rightRayEnd = rightRayRenderer.GetPosition(1);
            Vector3 rightRayDirection = rightRayStart - rightRayEnd;

            // if (
            //     ClosestPointsOnTwoLinesV1(out leftRayClosestPointV1,
            //     out rightRayClosestPointV1,
            //     leftRayStart,
            //     leftRayEnd,
            //     rightRayStart,
            //     rightRayEnd,
            //     -5,
            //     5,
            //     100)
            // )
            // {
            //     forceV1.transform.position =
            //         Vector3
            //             .Lerp(leftRayClosestPointV1,
            //             rightRayClosestPointV1,
            //             0.5f);
            //     forceV1.SetActive(true);
            // }
            rotationMode = false;
            bool gripValue;
            if (
                device
                    .TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton,
                    out gripValue) &&
                gripValue &&
                selectedObject
            )
            {
                RotationSphere (leftRayStart, rightRayStart);
            }

            if (
                ClosestPointsOnTwoLinesV2(out leftRayClosestPointV2,
                out rightRayClosestPointV2,
                leftRayStart,
                leftRayDirection,
                rightRayStart,
                rightRayDirection) &&
                !rotationMode
            )
            {
                Vector3 position =
                    Vector3
                        .Lerp(leftRayClosestPointV2,
                        rightRayClosestPointV2,
                        0.5f);
                forceThreshold.transform.position = position;

                forceThreshold.SetActive(true);
                float distance =
                    Vector3
                        .Distance(leftRayClosestPointV2,
                        rightRayClosestPointV2);

                if (distance < 0.4f)
                {
                    float size = -distance + 0.5f;

                    forceV2.transform.position = position;
                    forceV2.transform.localScale =
                        new Vector3(size, size, size);

                    if (distance < 0.2f)
                    {
                        forceV2.transform.localScale =
                            new Vector3(0.3f, 0.3f, 0.3f);

                        // select collidered object
                        if (selectedObject == null)
                        {
                            SelectObject(collisionDetector.collidedObject);
                        }
                    }
                    else
                    {
                        DeselectObject();
                    }
                    forceV2.SetActive(true);
                }
            }
        }
    }

    // This function finds the 2 closest points to each other on skew 2 rays.
    // Input parameters are the line start and end points.
    // An additional input paramater is a delta, which defines the delta low and hight of the points on the rays which will be compared in distance.
    public static bool
    ClosestPointsOnTwoLinesV1(
        out Vector3 leftRayClosestPointV1,
        out Vector3 rightRayClosestPointV1,
        Vector3 leftRayStart,
        Vector3 leftRayEnd,
        Vector3 rightRayStart,
        Vector3 rightRayEnd,
        int deltaLow,
        int deltaHigh,
        int pointsOnRay
    )
    {
        leftRayClosestPointV1 = Vector3.zero;
        rightRayClosestPointV1 = Vector3.zero;

        Vector3[] rightRayPoints = new Vector3[pointsOnRay];
        Vector3[] leftRayPoints = new Vector3[pointsOnRay];

        float distance = 0f;
        float shortestDistance = 100f;

        int indexR = 0;
        int indexL = 0;

        // store point on ray in arrays
        for (int i = 0; i < pointsOnRay; i++)
        {
            float interpolant = (float) i / (float) pointsOnRay;
            leftRayPoints[i] =
                Vector3.Lerp(leftRayStart, leftRayEnd, interpolant);
            rightRayPoints[i] =
                Vector3.Lerp(rightRayStart, rightRayEnd, interpolant);
        }

        // calculate distance of points on array by index ± delta
        for (int j = 0; j < pointsOnRay; j++)
        {
            for (int d = deltaLow; d <= deltaHigh; d++)
            {
                // check if index is not out of bounce
                if (j + d > 0 && j + d <= pointsOnRay - 1)
                {
                    distance =
                        Vector3
                            .Distance(leftRayPoints[j], rightRayPoints[j + d]);
                    if (distance < shortestDistance && distance > 0.01f)
                    {
                        shortestDistance = distance;
                        indexL = j;
                        indexR = j + d;
                    }
                }
            }
        }

        if (indexL != 0 && indexR != 0 && indexL != 99 && indexR != 99)
        {
            leftRayClosestPointV1 = leftRayPoints[indexL];
            rightRayClosestPointV1 = rightRayPoints[indexR];
            return true;
        }

        return false;
    }

    // This function finds the 2 closest points to each other on skew 2 rays.
    // Input parameters are the line points and directions of the rays.
    // If the lines are not parallel, the function outputs true, otherwise false.
    // This function is based on this unity forum discussion: https://answers.unity.com/questions/660369/how-to-convert-python-in-to-c-maths.html
    // which is based on this explanaition video https://www.youtube.com/watch?v=HC5YikQxwZA
    public static bool
    ClosestPointsOnTwoLinesV2(
        out Vector3 leftRayClosestPointV2,
        out Vector3 rightRayClosestPointV2,
        Vector3 leftRayStart,
        Vector3 leftRayDirection,
        Vector3 rightRayStart,
        Vector3 rightRayDirection
    )
    {
        leftRayClosestPointV2 = Vector3.zero;
        rightRayClosestPointV2 = Vector3.zero;

        float a = Vector3.Dot(leftRayDirection, leftRayDirection);
        float b = Vector3.Dot(leftRayDirection, rightRayDirection);
        float e = Vector3.Dot(rightRayDirection, rightRayDirection);

        float d = a * e - b * b;

        //lines are not parallel
        if (d != 0.0f)
        {
            Vector3 r = leftRayStart - rightRayStart;
            float c = Vector3.Dot(leftRayDirection, r);
            float f = Vector3.Dot(rightRayDirection, r);

            float s = (b * f - c * e) / d;
            float t = (a * f - c * b) / d;

            leftRayClosestPointV2 = leftRayStart + leftRayDirection * s;
            rightRayClosestPointV2 = rightRayStart + rightRayDirection * t;

            return true;
        }
        else
        {
            return false;
        }
    }

    private void SelectObject(GameObject go)
    {
        selectedObject = go;

        // check for collision
        if (collisionDetector.collided && selectedObject)
        {
            MeshRenderer selectedObjectRenderer =
                selectedObject.GetComponent<MeshRenderer>();
            selectedObjectRenderer
                .material
                .SetColor("_Color", forceHighlightColor);

            selectedObject.transform.SetParent(forceV2.transform, true);
        }
    }

    private void DeselectObject()
    {
        if (selectedObject)
        {
            selectedObject.transform.SetParent(scene.transform, true);
            MeshRenderer selectedObjectRenderer =
                selectedObject.GetComponent<MeshRenderer>();
            selectedObjectRenderer.material.SetColor("_Color", standardColor);
        }
        collisionDetector.collided = false;
        collisionDetector.collidedObject = null;

        selectedObject = null;
    }

    private void RotationSphere(Vector3 leftRayStart, Vector3 rightRayStart)
    {
        rotationMode = true;
        forceV2.SetActive(false);

        MeshRenderer selectedObjectRenderer =
            selectedObject.GetComponent<MeshRenderer>();
        selectedObjectRenderer.material.SetColor("_Color", rotationColor);

        // create a sphere in the middle of the two between the two rays on index 0
        Vector3 position = Vector3.Lerp(leftRayStart, rightRayStart, 0.5f);

        rotationLineRenderer.startWidth = 0.01f;
        rotationLineRenderer.positionCount = 2;
        rotationLineRenderer.SetPosition(0, leftRayStart);
        rotationLineRenderer.SetPosition(1, rightRayStart);
        rotationLineRenderer.useWorldSpace = false;
        rotationSphere.transform.position = position;

        selectedObject.transform.eulerAngles =
            rotationLineRenderer.transform.eulerAngles;
        rotationSphere.SetActive(true);
    }
}
