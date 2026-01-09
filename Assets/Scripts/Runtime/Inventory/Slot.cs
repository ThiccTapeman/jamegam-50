using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class Slot
{
    [field: SerializeField] public int Amount { get; private set; }
    [field: SerializeField] public float Cooldown { get; private set; }

    [SerializeField] private string effectName;
    [SerializeField] private Image cooldownFade;
    [SerializeField] private TMP_Text amountCounter;

    private float cooldownTimer;

    public bool TryUse()
    {
        if(cooldownTimer >= Cooldown && Amount > 0)
        {
            Amount--;
            cooldownTimer = 0;

            //FruitEffectManager.GetInstance().GetFruitEffect(effectName);
            Debug.Log($"boom {effectName}");

            UpdateUI();

            return true;
        }

        return false;
    }

    public void ColldownTick()
    {
        cooldownTimer += Time.deltaTime;
    }

    private void UpdateUI()
    {
        amountCounter.text = $"{effectName}: {Amount}";
        cooldownFade.StartCoroutine(AnimateCooldown());
    }

    private IEnumerator AnimateCooldown()
    {
        while (cooldownTimer < Cooldown)
        {
            cooldownFade.fillAmount = cooldownTimer / Cooldown;
            yield return null;
        }

        cooldownFade.fillAmount = 1;
    }
}