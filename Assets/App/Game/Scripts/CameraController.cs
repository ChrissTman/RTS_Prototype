using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// PC Controls and interactions
/// Box selection and HUD inputs
/// Ray casting to the environment
/// Camera movement controlled by mouse (screen edge detection)
/// </summary>
public class CameraController : MonoBehaviour
{
    [SerializeField] ManagerContext context;

    [SerializeField] LayerMask groundMask;
    [SerializeField] float scrollSensitivity;

    [Header("Camera movement")]
    [SerializeField] float camSpeed;
    [SerializeField] float smoothing; 
    [SerializeField] float edgeSize;
    [SerializeField] float yPosSmoothing;
    [SerializeField] float yPosStrategic;
    [SerializeField] float yPosTactical;
    [SerializeField] float speedBoost = 3;

    //[SerializeField] LayerMask mapMask;


    [Header("Camera boundary")]
    [SerializeField] float boundaryX; //both -+
    [SerializeField] float boundaryZ; //both -+

    bool isBoxSelecting;

    void Awake()
    {
        RefreshSelectionMode();
    }
    
    void Update()
    {
        MoveCamera();
        UpdateYPos();

        userOffsetY += -Input.mouseScrollDelta.y * scrollSensitivity * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.F))
        {
            Vector3 position = Vector3.zero;
            var platoon = context.PlayerActions.SelectedPlatoon;
            if(platoon != null && platoon.Alive > 0)
            {
                var squad = platoon.Squads.First(x => x.Alive > 0);
                foreach(var x in squad.Units)
                {
                    if (x.IsAlive)
                    {
                        position += x.WorldPosition;
                    }
                }

                position /= squad.Alive;
                transform.position = position + Vector3.up * (offsetY + userOffsetY) - Vector3.forward * 15;
            }

        }
    }
    

    /// <summary>
    /// Inputs a drag and normalizes it to Top-left to Bottom-right.
    /// If the input is from Top-Right to Bottom-left and it's not normalized, it creates issues.
    /// </summary>
    /// <param name="start">Drag start in pixels</param>
    /// <param name="end">Drag end in pixels</param>
    /// <returns>Normalized start and end points in pixels</returns>
    Tuple<Vector2, Vector2> NormalizeDrag(Vector2 start, Vector2 end)
    {
        Vector2 a = new Vector2(
            start.x > end.x ? end.x : start.x,
            start.y > end.y ? end.y : start.y
        );
        Vector2 b = new Vector2(
            start.x > end.x ? start.x : end.x,
            start.y > end.y ? start.y : end.y
        );
        return new Tuple<Vector2, Vector2>(a, b);
    }
    
    /// <summary>
    /// Checks if mouse position is on any screen edge and move along it
    /// </summary>
    void MoveCamera()
    {
        Vector2 newOffset = Vector2.zero;
        Vector3 mousePos = Input.mousePosition;

        float multiplier = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? speedBoost : 1f;

        //Player can't move while performing box selection
        if (!isBoxSelecting)
        {
            //X +
            if ( IsScreenEdge(Screen.width, mousePos.x, true, true)   &&
                 CanMoveAlongAxis(transform.position.x, boundaryX, true))
                newOffset.x = 1;

            //X -
            else if (IsScreenEdge(Screen.width, mousePos.x, false, true)  &&
                     CanMoveAlongAxis(transform.position.x, boundaryX, false))
                newOffset.x = -1;

            //Y +
            if (IsScreenEdge(Screen.height, mousePos.y, true, false)  &&
                CanMoveAlongAxis(transform.position.z, boundaryZ, true))
                newOffset.y = 1;

            //Y -
            else if (IsScreenEdge(Screen.height, mousePos.y, false, false) &&
                     CanMoveAlongAxis(transform.position.z, boundaryZ, false))
                newOffset.y = -1;
            
            //Arrow keys
            //X +
            if (Input.GetKey(KeyCode.RightArrow) &&
                CanMoveAlongAxis(transform.position.x, boundaryX, true))
                newOffset.x = 1;

            //X -
            else if (Input.GetKey(KeyCode.LeftArrow) &&
                     CanMoveAlongAxis(transform.position.x, boundaryX, false))
                newOffset.x = -1;

            //Y +
            if (Input.GetKey(KeyCode.UpArrow) &&
                CanMoveAlongAxis(transform.position.z, boundaryZ, true))
                newOffset.y = 1;

            //Y -
            else if (Input.GetKey(KeyCode.DownArrow) &&
                     CanMoveAlongAxis(transform.position.z, boundaryZ, false))
                newOffset.y = -1;
            
        }

        var positionTarget = transform.position;
        var speed = camSpeed * multiplier;
        positionTarget += transform.right * newOffset.x * speed;
        positionTarget += Vector3.Cross(transform.right, Vector3.up) * newOffset.y * speed;

        //transform.position = positionTarget;

        var delta = Time.unscaledDeltaTime;  //deltaTime / (Time.timeScale == 0 ? 1 : Time.timeScale);
        transform.position = Vector3.Lerp(transform.position, positionTarget, smoothing * delta);
    }

    /// <summary>
    /// Is the camera on the edge of the play area. 
    /// Can it move more along this axis in negative or positive direction.
    /// </summary>
    /// <param name="value">Position of the camera along one axis</param>
    /// <param name="max">Maximal position along one axis</param>
    /// <param name="positive">are we checking the minimum or the maximum</param>
    /// <returns></returns>
    bool CanMoveAlongAxis(float value, float max, bool positive)
    {
        if (positive)
            return value < max;
        else
            return -max < value;
    }

    /// <summary>
    /// Is mouse on the edge of screen.
    /// </summary>
    /// <param name="size">Total size of pixels along one axis</param>
    /// <param name="value">Mouse position along one axis</param>
    /// <param name="positive">Are we checking the positive side or the negative side(0 or 1920)</param>
    /// <param name="isWidth">Is the axis the Width</param>
    /// <returns></returns>
    bool IsScreenEdge(int size, float value, bool positive, bool isWidth)
    {
        //if the cursor is out of the window, don't move the camera
        //good for design time - editor
        if (value < -10 || value > size + 10)
            return false;

        //ration between the screen size and the value.
        float ration = 0;
        if (isWidth)
            ration = size / 100;
        else
            ration = (size / 100) * (Screen.width / Screen.height);

        //correct threshold to exceed
        var edge = ration * edgeSize;

        if (positive)
            return value >= size - edge;
        else
            return value <= edge;
    }

    public void RefreshSelectionMode()
    {
        updatingYPos = true;
    }

    RaycastHit hit;
    bool updatingYPos;
    float originalY;
    float offsetY;
    float userOffsetY;
    void UpdateYPos()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out hit, 999, groundMask))
            originalY = hit.point.y;

        float targetY = context.GameManager.GameMode == GameMode.Tactical ? yPosStrategic : yPosTactical;

        if (Mathf.Abs(offsetY - targetY) < 0.1f)
            updatingYPos = false;
        else
            offsetY = Mathf.Lerp(offsetY, targetY, Time.unscaledDeltaTime * yPosSmoothing);

        var pos = transform.position;
        pos.y = originalY + offsetY + userOffsetY;
        transform.position = pos;

    }
}