using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PCInput : MonoBehaviour
{
    [Header("Context")]
    [SerializeField] ManagerContext context;
    [SerializeField] Camera playerCamera;

    [Header("Raycasting")]
    [SerializeField] float maxDistance;
    [SerializeField] LayerMask selectMask;
    [SerializeField] LayerMask groundMask;

    [Header("Biases")]
    [SerializeField] float selectBias;

    [Header("Debug")]
    [SerializeField] Color lineColor;


    Vector3 mousePos => Input.mousePosition;

    //bool mortarMode => context.ArtilleryManager.MortarMode;
    bool forwardingMode => context.ForwardingManager.ForwardingMode;

    PlayerActions playerActions => context.PlayerActions;

    Material lineMaterial;

    /// <summary>
    /// Is the cursor on UI = should we ignore it?
    /// If user clicks on an UI button it still triggers GetKeyDown.
    /// </summary>
    bool cursorOverUI { get { return UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(); } }

    private void Awake()
    {
        CreateDragMaterial();
    }

    void Update()
    {
        //Ignore all frames when the cursor is hovering over UI.
        if (cursorOverUI)
            return;

        //if(mortarMode)
        //{
        //    HandleMortar();
        //}
        if (forwardingMode)
        {
            HandleForwarding();
        }
        else //Normal
        {
            LMB();
            RMB();

            //MMB - Temp dev
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                var pos = GetGround(Input.mousePosition);

                context.UnitManager.SpawnPlatoon(pos, PlatoonType.Rifle);
                context.UIManager.UpdateHudInfo();
            }
            //MMB - Temp dev
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                var pos = GetGround(Input.mousePosition);

                context.UnitManager.SpawnPlatoon(pos, PlatoonType.Explosive);
                context.UIManager.UpdateHudInfo();
            }
            
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                var pos = GetGround(Input.mousePosition);
            
                context.UnitManager.SpawnPlatoon(pos, PlatoonType.Artillery);
                context.UIManager.UpdateHudInfo();
            }
        }
    }


    #region ForwardingMode
    void HandleForwarding()
    {
        var groundPos = GetGround(mousePos);
        context.ForwardingManager.UpdateMarker(groundPos);

        if (Input.GetKeyUp(KeyCode.Mouse0))
            context.ForwardingManager.Submit();
    }
    #endregion

    #region NormalMode
    void LMB()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {

        }
        else if (Input.GetKey(KeyCode.Mouse0))
        {

        }
        else if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            var selectable = GetInterface<ISelectable>(mousePos);

            if (selectable is Unit)
            {
                var unit = selectable as Unit;
                var platoon = unit.Squad.Platoon;

                //var selectRecon = squad.ReconSquadActive ? squad.ReconUnits.Contains(unit) : false;
                playerActions.Select(platoon);
            }
            else if (selectable != null)
                playerActions.Select(selectable);
            else
                playerActions.ClearSelection();
        }
    }

    Vector2 dragStart_MousePos;
    Vector2 dragEnd_MousePos;
    Vector3 dragStart_Pos;
    Vector3 dragEnd_Pos;

    bool dragStart_AimedAtInteractable;

    bool isRightMouse;
    void RMB()
    {
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            isRightMouse = true;
            dragStart_Pos = GetGround(mousePos);
            dragStart_MousePos = dragEnd_MousePos = Input.mousePosition;

            var interactable = GetInterface<IInteractable>(mousePos);
            dragStart_AimedAtInteractable = interactable != null;
        }
        else if (Input.GetKey(KeyCode.Mouse1))
        {
            isRightMouse = true;
            dragEnd_Pos = GetGround(mousePos);
            dragEnd_MousePos = Input.mousePosition;
        }
        else if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            isRightMouse = false;
            dragEnd_Pos = GetGround(mousePos);
            dragEnd_MousePos = Input.mousePosition;

            var interactable = GetInterface<IInteractable>(mousePos);

            var target = dragStart_AimedAtInteractable ? interactable : null;
            playerActions.Action(target, dragStart_Pos, dragEnd_Pos);
        }
    }

    void OnRenderObject()
    {
        if (!isRightMouse)
            return;
        
        GL.PushMatrix();

        lineMaterial.SetPass(0);

        // Set transformation matrix for drawing to
        // match our transform
        //GL.MultMatrix(transform.localToWorldMatrix); //??
        GL.LoadOrtho();

        GL.Begin(GL.LINES);
        GL.Color(lineColor);

        GL.Vertex3(dragStart_MousePos.x / Screen.width, dragStart_MousePos.y / Screen.height, 0);
        GL.Vertex3(dragEnd_MousePos.x / Screen.width, dragEnd_MousePos.y / Screen.height, 0);

        GL.End();
        GL.PopMatrix();

        //Debug.Log("Drawing");
    }

    //Source: https://docs.unity3d.com/ScriptReference/GL.html
    void CreateDragMaterial()
    {
        // Unity has a built-in shader that is useful for drawing
        // simple colored things.
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        lineMaterial = new Material(shader);
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        // Turn on alpha blending
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        // Turn backface culling off
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        // Turn off depth writes
        lineMaterial.SetInt("_ZWrite", 0);
    }

    #endregion


    #region raycasts
    RaycastHit[] hitBuffer = new RaycastHit[1] { new RaycastHit() };

    T GetInterface<T>(Vector2 cursorPos) where T : class
    {
        var ray = playerCamera.ScreenPointToRay(cursorPos);
        var go = SphereCast(ray, selectBias, selectMask);

        if(go != null)
        {
            T selectable = go.GetComponentInParent<T>();
            if(selectable != null)
                return selectable;
            //else
            //    Debug.LogError($"{go.name} is in layer InteractableObject, but does not have any {typeof(T).Name} classes on it");
        }
        
        //default
        return null;
    }

    public Vector3 GetGround(Vector2 cursorPos)
    {
        var ray = playerCamera.ScreenPointToRay(cursorPos);
        return GroundRaycast(ray);
    }

    GameObject SphereCast(Ray ray, float radius, LayerMask mask)
    {
        if (Physics.SphereCastNonAlloc(ray, radius, hitBuffer, maxDistance, mask) > 0)
        {
            var result = hitBuffer[0];
            var go = result.collider.gameObject;

            return go;
        }

        return null;
    }
    Vector3 GroundRaycast(Ray ray)
    {
        if(Physics.RaycastNonAlloc(ray, hitBuffer, maxDistance, groundMask) > 0)
        {
            return hitBuffer[0].point;
        }

        return Vector3.zero;
    }
    #endregion
}
