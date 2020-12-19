using System.Collections;
using UnityEngine;

public class SpawnedGolfBall : MonoBehaviour
{
    public GameObject golfBallPrefab;
    public Transform spawnPosition;

    private bool ballPresent = false;
    private bool aboutToSpawnBall = false;

    //   private void Start()
    //   {
    //       //instantiateBall();
    //   }

    private void Update()
    {
        if (!ballPresent && !aboutToSpawnBall)
        {
            aboutToSpawnBall = true;
            StartCoroutine(SpawnGolfBall());
        }
        ballPresent = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("GolfBall"))
            ballPresent = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("GolfBall"))
            ballPresent = true;
    }

    // void OnTriggerExit(Collider other)
    // {
    //     if (other.gameObject.CompareTag("GolfBall"))
    //     {
    //         if (!aboutToSpawnBall)
    //         {
    //             aboutToSpawnBall = true;
    //             StartCoroutine(spawnGolfBall());
    //         }
    //     }
    // }

    private void InstantiateBall()
    {
        GameObject golfBall = Instantiate(golfBallPrefab);
        //golfBall.transform.parent = GameObject.Find("TrashObjects").transform;
        golfBall.transform.position = spawnPosition.position;
        aboutToSpawnBall = false;
    }

    private IEnumerator SpawnGolfBall()
    {
        yield return new WaitForSeconds(0.35f);
        if (aboutToSpawnBall)
            InstantiateBall();
    }

    private void OnEnable()
    {
        InstantiateBall();
    }
}