using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AmmoTypeUI : MonoBehaviour
{
    [SerializeField] Text nameLabel;
    [SerializeField] Text ammoLabel;
    [SerializeField] Button buyButton;

    public void SetData(string name, int currentAmount, Action onClick)
    {
        nameLabel.text = name;
        ammoLabel.text = $"{currentAmount}";

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => onClick());
    }
}
