using UnityEngine;

internal class Gems : MonoBehaviour
{
    [SerializeField] private int score = 5;

    // �������� �� ����� ����
    private LevelManager levelManager;

    // ������� ��� ��������� ���������
    public void SetLevelManager(LevelManager levelManager)
    {
        this.levelManager = levelManager;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<PlayerMovement>(out var player))
        {
            // ������� ����
            levelManager.UpdateScore(score);
            // �������� ������� ������, ����� ������ ���� ��� ��� ������� ����.
            gameObject.SetActive(false);
        }
    }
}