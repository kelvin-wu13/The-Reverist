using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    public enum EnemyBehaviorMode { Normal, Demo, Dummy }
    private EnemyBehaviorMode currentMode;

    private List<Enemy> enemies = new List<Enemy>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.Contains("Demo"))
        {
            currentMode = EnemyBehaviorMode.Demo;
        }
        else if (sceneName.Contains("Training"))
        {
            currentMode = EnemyBehaviorMode.Dummy;
        }
        else
        {
            currentMode = EnemyBehaviorMode.Normal;
        }
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (!enemies.Contains(enemy))
        {
            enemies.Add(enemy);
            ApplyBehaviorMode(enemy);
        }
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        enemies.Remove(enemy);
    }

    private void ApplyBehaviorMode(Enemy enemy)
    {
        switch (currentMode)
        {
            case EnemyBehaviorMode.Normal:
                enemy.SetBehavior(true, true);
                break;

            case EnemyBehaviorMode.Demo:
                enemy.SetBehavior(false, false);
                break;

            case EnemyBehaviorMode.Dummy:
                enemy.SetBehavior(false, false);
                break;
        }
    }

    public List<Enemy> GetAllEnemies()
    {
        return enemies.Where(e => e != null).ToList();
    }

    public IEnumerator WaitUntilAllStopped(float maxWaitTime)
    {
        float elapsed = 0f;
        while (elapsed < maxWaitTime)
        {
            if (GetAllEnemies().All(e => !e.IsMoving()))
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}