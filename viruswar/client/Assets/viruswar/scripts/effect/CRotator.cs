using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CRotator : MonoBehaviour {

    [SerializeField]
    float duration;

    [SerializeField]
    float angle;

    float direction;
    Quaternion from_rotation;
    Quaternion to_rotation;
    float time;


    void OnEnable()
    {
        this.from_rotation = Quaternion.AngleAxis(this.angle * -1.0f, Vector3.forward);
        this.to_rotation = Quaternion.AngleAxis(this.angle, Vector3.forward);
        this.direction = 1.0f;
        this.time = 0.0f;
    }


    void Update()
    {
        this.time += Time.deltaTime;
        if (this.time >= duration)
        {
            this.time = 0.0f;
            this.from_rotation = Quaternion.AngleAxis(this.angle * this.direction, Vector3.forward);
            this.direction *= -1.0f;
            this.to_rotation = Quaternion.AngleAxis(this.angle * this.direction, Vector3.forward);
        }

        transform.rotation = Quaternion.Slerp(this.from_rotation, this.to_rotation, this.time / this.duration);
    }


    public void play()
    {
        this.enabled = true;
    }


    public void stop()
    {
        transform.rotation = Quaternion.identity;
        this.enabled = false;
    }
}
