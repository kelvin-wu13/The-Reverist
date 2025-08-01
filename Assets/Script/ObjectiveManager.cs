using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections;
using TMPro;

[System.Serializable]
public class SimpleObjective
{
    public string skillCombination;
    public string description;
    [HideInInspector] public bool isCompleted = false;
    [HideInInspector] public GameObject uiElement;
    [HideInInspector] public Image statusIcon;
}

[System.Serializable]
public class SceneObjectives
{
    public string sceneName;
    public List<SimpleObjective> objectives = new List<SimpleObjective>();
    public string nextSceneName;
}

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    [Header("Objective Configuration")]
    [SerializeField] private List<SceneObjectives> allSceneConfigurations = new List<SceneObjectives>();

    [Header("UI References")]
    [SerializeField] private GameObject objectivePanel;
    [SerializeField] private Transform objectiveContainer;
    [SerializeField] private GameObject objectiveItemPrefab;

    [Header("Objective Sprites")]
    [SerializeField] private Sprite incompleteSprite;
    [SerializeField] private Sprite completedSprite;

    [SerializeField] private float transitionDelay = 3f;

    private List<SimpleObjective> currentObjectives = new List<SimpleObjective>();
    private string nextSceneName = "";
    private bool allObjectivesCompleted = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeObjectivesForScene();
        if (currentObjectives.Count > 0)
        {
            CreateObjectiveUI();
        }
        else if (objectivePanel != null)
        {
            objectivePanel.SetActive(false);
        }
    }

    private void InitializeObjectivesForScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneObjectives config = allSceneConfigurations.FirstOrDefault(sc => sc.sceneName == currentSceneName);

        if (config != null)
        {
            currentObjectives = config.objectives;
            nextSceneName = config.nextSceneName;
        }
    }

    private void CreateObjectiveUI()
    {
        if (objectivePanel == null || objectiveContainer == null || objectiveItemPrefab == null) return;

        foreach (Transform child in objectiveContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var objective in currentObjectives)
        {
            GameObject itemUI = Instantiate(objectiveItemPrefab, objectiveContainer);
            ObjectiveItemUI uiController = itemUI.GetComponent<ObjectiveItemUI>();

            if (uiController != null && uiController.statusIcon != null)
            {
                objective.statusIcon = uiController.statusIcon;
                objective.statusIcon.sprite = incompleteSprite;
            }

            if (uiController != null && uiController.objectiveText != null)
            {
                uiController.objectiveText.text = objective.description;
            }
            objective.uiElement = itemUI;
        }

        objectivePanel.SetActive(true);
    }

    private void CompleteObjective(string skillCombination)
    {
        if (allObjectivesCompleted) return;

        var objective = currentObjectives.FirstOrDefault(obj => obj.skillCombination == skillCombination && !obj.isCompleted);

        if (objective != null)
        {
            objective.isCompleted = true;
            UpdateObjectiveUI(objective);

            if (currentObjectives.All(obj => obj.isCompleted))
            {
                StartCoroutine(HandleAllObjectivesCompleted());
            }
        }
    }

    public void OnSkillCast(string skillCombination)
    {
        if (allObjectivesCompleted) return;

        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == "SaberDemo" &&
            (skillCombination == "WE" || skillCombination == "WQ" || skillCombination == "WW"))
        {
            CompleteObjective(skillCombination);
        }
        if (currentSceneName == "InterfereDemo" &&
            (skillCombination == "EQ" || skillCombination == "EW"))
        {
            CompleteObjective(skillCombination);
        }
    }

    public void OnSkillHitEnemy(string skillCombination)
    {
        if (allObjectivesCompleted) return;

        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == "SaberDemo" &&
            (skillCombination == "WE" || skillCombination == "WQ" || skillCombination == "WW"))
        {
            return;
        }

        CompleteObjective(skillCombination);
    }
    private void UpdateObjectiveUI(SimpleObjective objective)
    {
        ObjectiveItemUI uiController = objective.uiElement.GetComponent<ObjectiveItemUI>();

        if (uiController != null && uiController.statusIcon != null)
        {
            uiController.statusIcon.sprite = completedSprite;
            uiController.statusIcon.color = Color.green;
            StartCoroutine(AnimateCompletion(uiController.statusIcon));
        }

        if (uiController != null && uiController.objectiveText != null)
        {
            uiController.objectiveText.color = Color.green;
        }
    }

    private IEnumerator AnimateCompletion(Image statusIcon)
    {
        Vector3 originalScale = statusIcon.transform.localScale;
        float duration = 0.15f;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            statusIcon.transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.2f, t / duration);
            yield return null;
        }
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            statusIcon.transform.localScale = Vector3.Lerp(originalScale * 1.2f, originalScale, t / duration);
            yield return null;
        }
        statusIcon.transform.localScale = originalScale;
    }

    private IEnumerator HandleAllObjectivesCompleted()
    {
        allObjectivesCompleted = true;

        if (EventManager.Instance != null)
        {
            EventManager.Instance.MarkBattleOver();
            EventManager.Instance.CheckForBattleEnd();
        }

        yield return new WaitForSeconds(transitionDelay);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}