using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LevelTeleport : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject currentLevel;
    [SerializeField] private GameObject NextLevel;

    [SerializeField] private Transform startingPosition;
    [SerializeField] private Light2D globalLight;

    [SerializeField] private int newBallsCounter = 3;

    [SerializeField] private Color darkColor;
    [SerializeField] private Color NextLevelColor;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.TryGetComponent<PlayerMovement>(out var playerMovement))
        {
            if (playerMovement.IsEnteractive)
            {
                globalLight.color = darkColor;

                NextLevel.SetActive(true);
                player.transform.position = startingPosition.position;
                player.GetComponent<PlayerStatus>().SetBallsLeftCounter(newBallsCounter);
                currentLevel.SetActive(false);
                Flashlight[] flashlights = FindObjectsOfType<Flashlight>();
                for (int i = 0; i < flashlights.Length; ++i)
                {
                    flashlights[i].SetActive();
                }
                globalLight.color = NextLevelColor;
            }
        }
    }
}
