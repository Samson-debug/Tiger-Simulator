using System;
using System.Collections.Generic;
using System.Net.Mime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    //Singleton
    public static LevelManager instance;
    
    //events
    public static Action LevelChanged;
    [SerializeField] List<LevelPreyEntry> huntMap;
    [SerializeField] int levelIncreasePoints = 20;
    
    [Header("UI Refs")]
    public Image progressBar;
    public TextMeshProUGUI levelText;
 
    private int currentPoints;
    public static uint CurrentLevel{get; private set;}

    private void Awake()
    {
        if (instance == null){
            instance = this;
        }else
            Destroy(gameObject);
    }

    #region Event Sub/Unsub

    private void OnEnable()
    {
        FindFirstObjectByType<TigerController>().OnHunt += IncreasePoints;
    }

    #endregion
    private void Start()
    {
        CurrentLevel = 1;
        levelText.text = CurrentLevel.ToString();
    }

    public void IncreasePoints(int _amount)
    {
        currentPoints += _amount;

        if (currentPoints >= levelIncreasePoints){
            int extraPoints = currentPoints - levelIncreasePoints;

            CurrentLevel++;
            LevelChanged?.Invoke();
            levelText.text = CurrentLevel.ToString();
            Debug.Log("lvl changed");
            
            currentPoints = extraPoints;
        }
        
        progressBar.fillAmount = (float)currentPoints / levelIncreasePoints;
    }

    public bool CanKill(AnimalType _animalType)
    {
        List<AnimalType> attackableAnimalTypes = GetAnimalTypes(CurrentLevel);
        if (attackableAnimalTypes.Contains(_animalType))
            return true;

        return false;
    }

    private List<AnimalType> GetAnimalTypes(uint _level)
    {
        foreach (var l in huntMap)
            if (l.Level == _level) return l.AvailablePrey;
        
        return null;
    }

    public int RequiredLevel(AnimalType _animalType)
    {
        foreach (var l in huntMap){
            if(l.AvailablePrey.Contains(_animalType)) return l.Level;
        }

        return -1;
    }
}

