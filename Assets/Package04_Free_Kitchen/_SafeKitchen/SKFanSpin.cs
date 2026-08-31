using UnityEngine;

/// 벽 환풍기 로터를 로컬 Z축으로 돌린다. 속도는 SKData.FAN_SPIN 이 소유한다.
public class SKFanSpin : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(0f, 0f, SKData.FAN_SPIN * Time.deltaTime, Space.Self);
    }
}
