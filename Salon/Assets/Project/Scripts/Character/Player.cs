using UnityEngine;

public class Player : MonoBehaviour
{
    public string DisplayName { get; set; }
    public string UserId { get; set; }
    public Player(string displayName, string userId)
    {
        DisplayName = displayName;
        UserId = userId;
    }
    public void Move(Vector3 position)
    {
        transform.position = position;
    }

    public void Rotate(Vector3 rotation)
    {
        transform.rotation = Quaternion.Euler(rotation);
    }

    public void MoveTo()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 direction = new Vector3(horizontal, 0, vertical);
        Move(direction);
        Rotate(direction);
    }

}