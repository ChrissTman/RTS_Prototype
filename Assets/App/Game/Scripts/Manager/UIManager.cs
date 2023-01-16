using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// UI logic and calls.
/// Creation of buttons for squads.
/// HUD info.
/// Label creation and positioning.
/// </summary>
public class UIManager : MonoBehaviour
{
    //[Header("Dependencies")]
    [SerializeField] ManagerContext context;

    [SerializeField] Camera playerCamera;

    [Header("Game objects")]
    [SerializeField] GameObject hud;
    [SerializeField] GameObject overlay;

    [Header("Supply")]
    [SerializeField] GameObject supply;
    [SerializeField] PlatoonInfoUI platoonInfoUI;
    [SerializeField] Transform platoonSupplyRoot;
    [SerializeField] GameObject supplyActions;

    //Soldier upgrades
    [SerializeField] Button buyAirSupport;
    [SerializeField] Button buyArtillerySupport;
    [SerializeField] Button buyMortarSupport;
    [SerializeField] Button buySniperSupport;

    //Soldier ammo
    [SerializeField] AmmoTypeUI ammoTypeUI;
    [SerializeField] Transform ammoTypeRoot;

    [Header("Box selection")]
    [SerializeField] RectTransform boxSelect;

    [Header("Labels")]
    [SerializeField] int unitUIPoolSize; //amount
    [SerializeField] UnitUI labelPrefab;
    [SerializeField] Transform labelRoot;
    
    [SerializeField] Text hud_unitInfo;
    
    [Header("Select mode")]
    [SerializeField] Image selectionButtonImage;

    FixedPool<UnitUI> labelPool;
    UnitUI universalLabel;
    List<UnitUIData> labelData = new List<UnitUIData>();

    [Header("Squad buttons")]
    [SerializeField] SquadButton squadButton;
    [SerializeField] int squadButtonSize = 20;
    [SerializeField] SquadButtonHorizontal squadButtonHorizontal;
    [SerializeField] List<UnitIconPair> iconPairs;
    [SerializeField] List<PlatoonIconPair> platoonIconPairs;
    Dictionary<UnitType, Sprite> unitIcons = new Dictionary<UnitType, Sprite>();
    Dictionary<PlatoonType, Sprite> platoonIcons = new Dictionary<PlatoonType, Sprite>();

    [Header("Unit actions")]
    [SerializeField] List<GameObject> actionButtonGOs;

    [Header("Artillery")]
    [SerializeField] GameObject ArtillerView;

    [Header("Airstrike")]
    [SerializeField] Image airstrikeOverlay;

    FixedPool<SquadButton> squadButtonBuffer;

    Dictionary<int, SquadButton> currentSquadButtons = new Dictionary<int, SquadButton>();

    //Initiate
    void Start()
    {
        CreateActionsButtonData();
        CreateUnitIcons();
        CreatePlatoonIcons();

        DisableBoxSelection();
        BufferLabels();
        BufferSquadButtons();
    }


    #region Unit Actions
    List<ButtonData> actionButtons = new List<ButtonData>();
    Action<int> currentResponse;

    void CreateActionsButtonData()
    {
        for (int i = 0; i < actionButtonGOs.Count; i++)
        {
            var btnGO = actionButtonGOs[i];

            var bData = new ButtonData();
            bData.Button = btnGO.GetComponentInChildren<Button>();
            bData.Image = btnGO.GetComponentInChildren<Image>();

            bData.ValidateData();

            //int index = i;
            //bData.SetButtonCallback(() => OnActionClick(index));

            actionButtons.Add(bData);
        }
    }

    public void ResetActionUI()
    {
        for (int i = 0; i < actionButtons.Count; i++)
        {
            var image = actionButtons[i].Image;
            var btn = actionButtons[i].Button;

            image.sprite = null;
            btn.interactable = false;
        }
    }
    public void LoadActionsUIState(List<PlatoonActionInfo> infos, Action<int> response)
    {
        currentResponse = response;

        for (int i = 0; i < infos.Count; i++)
        {
            var buttonData = actionButtons[i];
            var image = buttonData.Image;
            var btn = buttonData.Button;


            var info = infos[i];
            buttonData.SetButtonCallback(() => OnActionClick(info.ActionIndex));
            image.sprite = info.Sprite;
            btn.interactable = true;
            
        }
    }

    void OnActionClick(int index)
    {
        currentResponse?.Invoke(index);
    }

    #endregion

    void CreateUnitIcons()
    {
        foreach(var x in iconPairs)
        {
            unitIcons.Add(x.UnitType, x.Sprite);
        }
    }

    void CreatePlatoonIcons()
    {
        platoonIcons = platoonIconPairs.ToDictionary(x => x.PlatoonType, x => x.Sprite);
    }

    void BufferSquadButtons()
    {
        squadButtonBuffer = new FixedPool<SquadButton>(squadButtonSize);
        for (int i = 0; i < squadButtonBuffer.BufferSize; i++)
        {
            SquadButton squadBtn = Instantiate(squadButton);
            squadButtonBuffer.SetAtIndex(i, squadBtn);
        }

        squadButtonHorizontal.Initialize(squadButtonBuffer.Buffer);
    }

    /// <summary>
    /// Fills buffer of a fixed size with new unit labels.
    /// </summary>
    void BufferLabels()
    {
        labelPool = new FixedPool<UnitUI>(unitUIPoolSize);
        for (int i = 0; i < labelPool.BufferSize; i++)
        {
            var label = Instantiate(labelPrefab);
            label.Deactivate();
            label.transform.SetParent(labelRoot);

            labelPool.SetAtIndex(i, label);
        }
    }

    /// <summary>
    /// De-selects all units.
    /// </summary>
    public void ClearUnitHighlight()
    {
        labelData.Clear();
    }

    public void HighlightPlatoons(FixedList<Platoon> platoons)
    {
        labelData.Clear();

        var team = context.GameManager.CurrentTeam;

        foreach(Slot<Platoon> slot in platoons)
        {
            if (!slot.Taken)
                continue;

            var platoon = slot.Element;
            
            if (platoon.Alive <= 0 || platoon.Team != team)
                continue;

            var squad = platoon.Squads[0];
            var pos = Vector3.zero;
            foreach (var x in squad.Units)
            {
                if (!x.IsAlive)
                    continue;

                pos += (x as IMovableUnit).WorldPosition;
            }
            pos = pos / squad.Alive;

            labelData.Add(new UnitUIData()
            {
                Position = playerCamera.WorldToScreenPoint(pos),
                Text = $"{platoon.Company.Prefix}{platoon.Index}",
                Highlight = context.PlayerActions.SelectedPlatoon == platoon,
            });
            
        }

        UpdateLabels();
    }

    /// <summary>
    /// Turns on labels for specific units.
    /// </summary>
    /// <param name="selected">Units to select.</param>
    public void HightlightSelected(FixedList<ISelectable> selected)
    {
        labelData.Clear();


        for (int i = 0; i < selected.BufferSize; i++)
        {
            var slot = selected.Buffer[i];
            if(slot.Taken)
            {
                var unit = slot.Element;
                var screenPos = playerCamera.WorldToScreenPoint(unit.WorldPosition);
                var procentile = unit.Percentage;

                //TODO: check performance
                labelData.Add(new UnitUIData()
                {
                    Position = screenPos,
                    Text = unit.Info,
                    Procentile = procentile,
                });
            }
        }
        
        UpdateLabels();
    }

    
    void UpdateLabels()
    {
        int dataIndex = 0;
        int recycledCount = 0;
        int currentlyActive = labelPool.GetTakenAmount();

        int amountToDisplay = labelData.Count;

        for (int i = 0; i < labelPool.BufferSize; i++)
        {
            var slot = labelPool.Buffer[i];
            if (slot.Taken && dataIndex < amountToDisplay)
            {
                recycledCount++;

                var label = slot.Element;
                SetLabel(label, labelData[dataIndex++]);
            }
        }

        int recycleOffset = 0;
        if(recycledCount < currentlyActive)
        {
            for (int i = 0; i < labelPool.BufferSize; i++)
            {
                var slot = labelPool.Buffer[i];
                if (slot.Taken)
                {
                    recycleOffset++;

                    if(recycleOffset > recycledCount)
                    {
                        slot.Element.Deactivate();
                        labelPool.ReturnElement(slot.Element);
                    }
                }

            }
        }
        
        if(amountToDisplay > recycledCount && dataIndex < amountToDisplay)
        {
            var label = labelPool.GetAvailable();
            if (label == null)
                Debug.LogError("Unit UI buffer exceeded!");
            else
            {
                SetLabel(label, labelData[dataIndex++]);
                label.Activate();
            }
        }
        
    }
    
    void SetLabel(UnitUI label, UnitUIData data)
    {
        label.transform.position = data.Position;
        label.SetText(data.Text);
        label.SetHealthIndicator(data.Procentile);
        label.SetHighlight(data.Highlight);
    }

    /// <summary>
    /// Updates the HUD info with specific string.
    /// </summary>
    /// <param name="text">text</param>
    public void UpdateHudInfo(string text)
    {
        hud_unitInfo.text = text;
    }

    /// <summary>
    /// Updates the HUD info with default values.
    /// </summary>
    /// <param name="text">text</param>
    public void UpdateHudInfo()
    {
        int unitsTotal = 0;
        foreach(Slot<Platoon> x in context.UnitManager.Platoons)
        {
            if (!x.Taken)
                continue;

            var plat = x.Element;
            unitsTotal += plat.Alive;
        }
        
        //GC?
        hud_unitInfo.text = string.Format("{0} units in {4} platoons\nTeam: {1}\nSelectionMode: {2}\nTimeScale: {3}",
                                         unitsTotal, context.GameManager.CurrentTeam, context.GameManager.GameMode,
                                         context.GameManager.TimeScale, context.UnitManager.Platoons.GetTakenAmount());
    }

    /// <summary>
    /// Positions the Start and End of the UI box selection.
    /// </summary>
    /// <param name="start">Position in pixels.</param>
    /// <param name="end">Position in pixels.</param>
    //TODO: fix folding on small box selections
    public void UpdateBoxSelection(Vector2 start, Vector2 end)
    {
        boxSelect.localPosition = start;

        var delta = start - end;

        var sizeDelta = boxSelect.sizeDelta;
        sizeDelta.x = Math.Abs(delta.x);
        sizeDelta.y = Math.Abs(delta.y);

        boxSelect.sizeDelta = sizeDelta;
    }

    /// <summary>
    /// Turns of the box selection.
    /// </summary>
    public void DisableBoxSelection()
    {
        boxSelect.sizeDelta = Vector2.zero;
    }

    /// <summary>
    /// Creates squad button, registers OnClick and selects correct color.
    /// Temp.
    /// </summary>
    /// <param name="text">Text of the button.</param>
    /// <param name="team">Team of the button.</param>
    /// <param name="OnClick">Callback.</param>
    public void CreateSquadButton(UnitType type, string text, Action OnClick, int squadID)
    {
        var btn = squadButtonBuffer.GetAvailable();

        if(btn == null)
        {
            Debug.LogError($"Squad button buffer is exceeded. Total size: {squadButtonBuffer.BufferSize}");
            return;
        }

        btn.SetState(true);
        btn.SetText(text);
        btn.SetIcon(unitIcons[type]);

        currentSquadButtons.Add(squadID, btn);

        btn.OnClick += OnClick;

        squadButtonHorizontal.UpdateOrder();
    }

    public void RemoveSquadButton(int squadID)
    {
        if(currentSquadButtons.TryGetValue(squadID, out SquadButton btn))
        {
            btn.SetState(false);
            squadButtonBuffer.ReturnElement(btn);

            squadButtonHorizontal.UpdateOrder();
        }
    }

    public void SetSquadTeam(Team team)
    {
        return;

        if (team == Team.TeamRed)
        {
            //squadParentBlack.gameObject.SetActive(true);
            //squadParentGreen.gameObject.SetActive(false);
        }
        else if (team == Team.TeamGreen)
        {
            //squadParentBlack.gameObject.SetActive(false);
            //squadParentGreen.gameObject.SetActive(true);
        }
    }

    public void SetUI(bool state)
    {
        return;

        hud.SetActive(state);
        overlay.SetActive(state);
    }

    public void SetArilleryView(bool state)
    {
        ArtillerView.SetActive(state);
    }

    public void UpdateAirstrikeOverlay(float f)
    {
        airstrikeOverlay.fillAmount = f;
    }

    public void SetSupplyUI(bool state)
    {
        supply.SetActive(state);
        if (state)
            FillPlatoons();
        else
            ClearPlatoons();
    }

    void ClearPlatoons()
    {
        supplyActions.SetActive(false);
        foreach (Transform x in platoonSupplyRoot)
        {
            Destroy(x.gameObject);
        }
    }

    void FillPlatoons()
    {
        var team = context.GameManager.CurrentTeam;
        var platoons = context.UnitManager.Platoons;
        foreach(Slot<Platoon> slot in platoons.Buffer)
        {
            if (!slot.Taken)
                continue;
            var platoon = slot.Element;

            if (platoon.Team != team || platoon.Type != PlatoonType.Rifle)
                continue;
            
            var sprite = platoonIcons[platoon.Type];

            var platInfo = Instantiate(platoonInfoUI, platoonSupplyRoot);
            var name = $"{platoon.Company.Prefix}{platoon.Index}";
            platInfo.SetData(() => SelectSupplyPlatoon(platoon), name, sprite);
        }
    }

    void SelectSupplyPlatoon(Platoon pl)
    {
        supplyActions.SetActive(true);

        var supply = context.SupplyManager;
        var flags = supply.GetUpgradeFlags(pl);

        buyAirSupport.interactable = !flags.HasAirForwarder;
        SetOnClick(buyAirSupport, () => flags.HasAirForwarder = true);

        buyArtillerySupport.interactable = !flags.HasArtilleryForwarder;
        SetOnClick(buyArtillerySupport, () => flags.HasArtilleryForwarder = true);
        
        buyMortarSupport.interactable = !flags.HasMortarForwarder;
        SetOnClick(buyMortarSupport, () => flags.HasMortarForwarder = true);
        
        buySniperSupport.interactable = !flags.HasSniperForwarder;
        SetOnClick(buySniperSupport, () => flags.HasSniperForwarder = true);

        foreach (Transform x in ammoTypeRoot)
        {
            Destroy(x.gameObject);
        }

        foreach (var ammunition in pl.Magazines)
        {
            var type = ammunition.Key;
            var ammo = ammunition.Value;

            var ammoType = Instantiate(ammoTypeUI, ammoTypeRoot);

            ammoType.SetData($"{type}", ammo, () =>
            {
                print("BUYYY!");
                pl.Magazines[type]++;
            });
        }
    }
    

    void SetOnClick(Button btn, Action action)
    {
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => action());
    }
}

public class UnitUIData
{
    public Vector3 Position;
    public string Text;
    public float Procentile;
    public bool Highlight;
}

[Serializable]
public class ButtonData
{
    public Button Button;
    public Image Image;

    public void SetButtonCallback(Action callback)
    {
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(new UnityAction(callback));
    }

    public void ValidateData()
    {
        if (Button == null)
            throw new Exception("Button == null");
        if (Image == null)
            throw new Exception("Image == null");
    }
}

[Serializable]
public class UnitIconPair
{
    public UnitType UnitType;
    public Sprite Sprite;
}

[Serializable]
public class PlatoonIconPair
{
    public PlatoonType PlatoonType;
    public Sprite Sprite;
}