using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    public float speed = 1.0f;
    public float resetPositionX = -20.0f;
    public float startPositionX = 20.0f;

    private void Update()
    {
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        if (transform.position.x <= resetPositionX)
        {
            Vector3 newPos = transform.position;
            newPos.x = startPositionX;
            transform.position = newPos;
        }
    }
}
