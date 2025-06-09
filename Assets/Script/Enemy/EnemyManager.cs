using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

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
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        enemies.Remove(enemy);
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
