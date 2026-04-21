using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("The pipe pair prefab to spawn")]
    [SerializeField] private GameObject pipePrefab;

    [Tooltip("Seconds between each pipe spawn")]
    [SerializeField] private float spawnInterval = 2.2f;

    [Tooltip("Minimum seconds between spawns (difficulty ramp)")]
    [SerializeField] private float minSpawnInterval = 1.2f;

    [Tooltip("How much to reduce spawn interval per second of play")]
    [SerializeField] private float difficultyRampRate = 0f; 

    private bool isSpawning = false;
    private float currentInterval;
    private float timer = 0f;
    private List<Pipe> activePipes = new List<Pipe>();

    private void Start()
    {
        currentInterval = spawnInterval;
    }

    public void SetSpawning(bool spawn)
    {
        isSpawning = spawn;
        if (spawn)
        {
            timer = currentInterval; 
        }
    }

    public void ClearAllPipes()
    {
        activePipes.RemoveAll(p => p == null);
        foreach (var pipe in activePipes)
        {
            if (pipe != null) Destroy(pipe.gameObject);
        }
        activePipes.Clear();
        currentInterval = spawnInterval;
        timer = 0f;
    }

    private void Update()
    {
        if (!isSpawning) return;

        timer += Time.deltaTime;

        currentInterval = Mathf.Max(minSpawnInterval, currentInterval - difficultyRampRate * Time.deltaTime);

        if (timer >= currentInterval)
        {
            timer = 0f;
            SpawnPipe();
        }
    }

    private void SpawnPipe()
    {
        if (pipePrefab == null)
        {
            Debug.LogError("[PipeSpawner] pipePrefab is not assigned! Drag the PipePair prefab in the Inspector.");
            return;
        }

        GameObject newPipeObj = Instantiate(pipePrefab, transform.position, Quaternion.identity);
        Pipe pipe = newPipeObj.GetComponent<Pipe>();

        if (pipe != null)
        {
            pipe.SetScrolling(true);
            activePipes.Add(pipe);
        }

        activePipes.RemoveAll(p => p == null);
    }
}
