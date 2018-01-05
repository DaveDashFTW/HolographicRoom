using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.SpatialMapping;

public class SpawnOnWall : MonoBehaviour {

    /// <summary>
    /// Indicates if processing of the surface meshes is complete.
    /// </summary>
    private bool meshesProcessed = false;

    // Use this for initialization
    void Start () {
        // Register for the MakePlanesComplete event.
        SurfaceMeshesToPlanes.Instance.MakePlanesComplete += SurfaceMeshesToPlanes_MakePlanesComplete;
    }
	
	// Update is called once per frame
	void Update () {
        if (!meshesProcessed)
        {
            if (SpatialMappingManager.Instance.IsObserverRunning())
            {
                // 3.a: If running, Stop the observer by calling
                // StopObserver() on the SpatialMappingManager.Instance.
                SpatialMappingManager.Instance.StopObserver();
            }

            // 3.a: Call CreatePlanes() to generate planes.
            CreatePlanes();

            // 3.a: Set meshesProcessed to true.
            meshesProcessed = true;
        }
    }

    private void CreatePlanes()
    {
        // Generate planes based on the spatial map.
        SurfaceMeshesToPlanes surfaceToPlanes = SurfaceMeshesToPlanes.Instance;
        if (surfaceToPlanes != null && surfaceToPlanes.enabled)
        {
            surfaceToPlanes.MakePlanes();
        }
    }

    /// <summary>
    /// Handler for the SurfaceMeshesToPlanes MakePlanesComplete event.
    /// </summary>
    /// <param name="source">Source of the event.</param>
    /// <param name="args">Args for the event.</param>
    private void SurfaceMeshesToPlanes_MakePlanesComplete(object source, System.EventArgs args)
    {
        // Collection of wall planes that we can use to set vertical items on.
        List<GameObject> vertical = new List<GameObject>();

        // Get all wall planes by calling
        // SurfaceMeshesToPlanes.Instance.GetActivePlanes().
        // Assign the result to the 'vertical' list.
        vertical = SurfaceMeshesToPlanes.Instance.GetActivePlanes(PlaneTypes.Wall);

        if (vertical.Count >= 1)
        {
            RemoveVertices(SurfaceMeshesToPlanes.Instance.ActivePlanes);

            GenerateItemsInWorld(vertical);
        }
        else
        {
            // We do not have enough floors/walls to place our holograms on...

            // 3.a: Re-enter scanning mode so the user can find more surfaces by 
            // calling StartObserver() on the SpatialMappingManager.Instance.
            SpatialMappingManager.Instance.StartObserver();

            // 3.a: Re-process spatial data after scanning completes by
            // re-setting meshesProcessed to false.
            meshesProcessed = false;
        }
    }

    public void GenerateItemsInWorld(List<GameObject> verticalSurfaces)
    {
        List<GameObject> verticalObjects = new List<GameObject>();

        List<int> UsedPlanes = new List<int>();

        // Sort the planes by distance to user.
        verticalSurfaces.Sort((lhs, rhs) =>
        {
            Vector3 headPosition = Camera.main.transform.position;
            Collider rightCollider = rhs.GetComponent<Collider>();
            Collider leftCollider = lhs.GetComponent<Collider>();

            // This plane is big enough, now we will evaluate how far the plane is from the user's head.  
            // Since planes can be quite large, we should find the closest point on the plane's bounds to the 
            // user's head, rather than just taking the plane's center position.
            Vector3 rightSpot = rightCollider.ClosestPointOnBounds(headPosition);
            Vector3 leftSpot = leftCollider.ClosestPointOnBounds(headPosition);

            return Vector3.Distance(leftSpot, headPosition).CompareTo(Vector3.Distance(rightSpot, headPosition));
        });


        int index = -1;
        Collider collider = gameObject.GetComponent<Collider>();
        index = FindNearestPlane(verticalSurfaces, collider.bounds.size, UsedPlanes, true);


        // If we can't find a good plane we will put the object floating in space.
        Vector3 position = Camera.main.transform.position + Camera.main.transform.forward * 2.0f + Camera.main.transform.right * (Random.value - 1.0f) * 2.0f;
        Quaternion rotation = Quaternion.identity;

        // If we do find a good plane we can do something smarter.
        if (index >= 0)
        {
            UsedPlanes.Add(index);
            GameObject surface = verticalSurfaces[index];
            SurfacePlane plane = surface.GetComponent<SurfacePlane>();
            position = surface.transform.position + (plane.PlaneThickness * plane.SurfaceNormal);
            position = AdjustPositionWithSpatialMap(position, plane.SurfaceNormal);
            rotation = Camera.main.transform.localRotation;

            // Vertical objects should face out from the wall.
            rotation = Quaternion.LookRotation(surface.transform.forward, Vector3.up);
        }

            
        GameObject mainMenu = Instantiate(gameObject, position, rotation) as GameObject;
        mainMenu.transform.parent = gameObject.transform;
    }

    /// <summary>
    /// Attempts to find a the closest plane to the user which is large enough to fit the object.
    /// </summary>
    /// <param name="planes">List of planes to consider for object placement.</param>
    /// <param name="minSize">Minimum size that the plane is required to be.</param>
    /// <param name="startIndex">Index in the planes collection that we want to start at (to help avoid double-placement of objects).</param>
    /// <param name="isVertical">True, if we are currently evaluating vertical surfaces.</param>
    /// <returns></returns>
    private int FindNearestPlane(List<GameObject> planes, Vector3 minSize, List<int> usedPlanes, bool isVertical)
    {
        int planeIndex = -1;

        for (int i = 0; i < planes.Count; i++)
        {
            if (usedPlanes.Contains(i))
            {
                continue;
            }

            Collider collider = planes[i].GetComponent<Collider>();
            if (isVertical && (collider.bounds.size.x < minSize.x || collider.bounds.size.y < minSize.y))
            {
                // This plane is too small to fit our vertical object.
                continue;
            }
            else if (!isVertical && (collider.bounds.size.x < minSize.x || collider.bounds.size.y < minSize.y))
            {
                // This plane is too small to fit our horizontal object.
                continue;
            }

            return i;
        }

        return planeIndex;
    }

    /// <summary>
    /// Adjusts the initial position of the object if it is being occluded by the spatial map.
    /// </summary>
    /// <param name="position">Position of object to adjust.</param>
    /// <param name="surfaceNormal">Normal of surface that the object is positioned against.</param>
    /// <returns></returns>
    private Vector3 AdjustPositionWithSpatialMap(Vector3 position, Vector3 surfaceNormal)
    {
        Vector3 newPosition = position;
        RaycastHit hitInfo;
        float distance = 0.5f;

        // Check to see if there is a SpatialMapping mesh occluding the object at its current position.
        if (Physics.Raycast(position, surfaceNormal, out hitInfo, distance, SpatialMappingManager.Instance.LayerMask))
        {
            // If the object is occluded, reset its position.
            newPosition = hitInfo.point;
        }

        return newPosition;
    }

    /// <summary>
    /// Removes triangles from the spatial mapping surfaces.
    /// </summary>
    /// <param name="boundingObjects"></param>
    private void RemoveVertices(IEnumerable<GameObject> boundingObjects)
    {
        RemoveSurfaceVertices removeVerts = RemoveSurfaceVertices.Instance;
        if (removeVerts != null && removeVerts.enabled)
        {
            removeVerts.RemoveSurfaceVerticesWithinBounds(boundingObjects);
        }
    }

    /// <summary>
    /// Called when the GameObject is unloaded.
    /// </summary>
    private void OnDestroy()
    {
        if (SurfaceMeshesToPlanes.Instance != null)
        {
            SurfaceMeshesToPlanes.Instance.MakePlanesComplete -= SurfaceMeshesToPlanes_MakePlanesComplete;
        }
    }
}
