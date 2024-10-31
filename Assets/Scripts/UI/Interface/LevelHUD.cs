using System.Collections;
using TMPro;
using UnityEngine;

// ����� ����������� ����������� �� ������
public partial class LevelHUD : MonoBehaviour
{
    // ������ �� ��������� ������ � ������� ������������ ���� ������
    [SerializeField] private TMP_Text scoreText;
    // �������� ������� ������
    public readonly UICommandQueue CommandQueue = new UICommandQueue();

    private void Start()
    {
        // ��� �������� ���������� �������� ������ �������
        // ��������� ����������� ������������ �������
        StartCoroutine(AsyncUpdate());
    }

    // ����� ��������� ������ �� �������
    private IEnumerator AsyncUpdate()
    {
        while (true)
        {
            // ������� ����� ������� �� �������
            if (CommandQueue.TryDequeueCommand(out var command))
            {
                switch (command)
                {
                    // � ����������� �� ���� ������� ������� ����� ����������
                    // ���� ����� ��������� ����� ����������� ������, � ��� ��� ����� ������������� � ����� �����.
                    case UpdateScoreCommand updateScoreCommand:
                        {
                            // ������� �����
                            scoreText.text = $"Score: {updateScoreCommand.NewScore}";
                            break;
                        }
                }
            }

            // ����������� ����� �������, ��� �� ��������� ���������� 
            // �� ���� ����� ���������� ������
            yield return null;
        }
    }

    private void OnDestroy()
    {
        // ��� ����������� ���������� ��������� ������
        StopAllCoroutines();
    }
}
