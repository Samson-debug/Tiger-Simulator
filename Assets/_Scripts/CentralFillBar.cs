using System;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;

public class CentralFillBar : MonoBehaviour
{
    public Image[] fills;
    
    [SerializeField] [Range(0f, 1f)] float fillAmount;

    private void OnValidate()
    {
        if(fills == null || fills.Length == 0) return;

        foreach (var fill in fills){
            fill.fillAmount = fillAmount;
        }
    }

    public void SetFillAmount(float _value)
    {
        fillAmount = Mathf.Clamp01(_value);
        
        foreach (var fill in fills){
            fill.fillAmount = fillAmount;
        }
    }
}
