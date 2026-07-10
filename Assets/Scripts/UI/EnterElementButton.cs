using UnityEngine;
using UnityEngine.SceneManagement;

public class EnterElementButton : MonoBehaviour
{
    public void Jump()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            GameObject managerObject = new GameObject("GameManager");
            gameManager = managerObject.AddComponent<GameManager>();
        }

        gameManager.ResetRunState();
        SceneManager.LoadScene("ElementSelectScene");
    }
}

