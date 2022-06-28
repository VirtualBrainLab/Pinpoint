using UnityEngine;
using UnityEngine.SceneManagement;

public class TP_LoadingScreen : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SceneManager.LoadScene("TrajectoryPlanner", LoadSceneMode.Single);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
