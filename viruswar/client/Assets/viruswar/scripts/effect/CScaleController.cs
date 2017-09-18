using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CScaleController : MonoBehaviour {

    [SerializeField]
    float duration;

    [SerializeField]
    Vector3 scale_from;

    [SerializeField]
    Vector3 scale_to;

    Vector3 initial_scale;
    float time;


    void Awake()
    {
        this.initial_scale = transform.localScale;
    }


    void LateUpdate()
    {
        this.time += Time.deltaTime;

        // 모바일(안드로이드)에서 안먹어서 Vector3.Lerp로 교체함. 원인은 아직 모름.
        //transform.localScale = easing_vector3(this.scale_from, this.scale_to, this.time / this.duration, EasingUtil.easeInQuad);
        transform.localScale = Vector3.Lerp(this.scale_from, this.scale_to, this.time / this.duration);
    }


    public void reset()
    {
        transform.localScale = this.initial_scale;
    }


    delegate float EasingMethod(float start, float from, float t);
    static Vector3 easing_vector3(Vector3 from, Vector3 to, float t, EasingMethod method)
    {
        t = Mathf.Clamp(t, 0.0f, 1.0f);
        return new Vector3(
            method(from.x, to.x, t),
            method(from.y, to.y, t),
            method(from.z, to.z, t));
    }
}
