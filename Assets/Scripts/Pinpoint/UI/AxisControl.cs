using UnityEngine;

public class AxisControl : MonoBehaviour
{
    public LineRenderer XAxis;
    public LineRenderer YAxis;
    public LineRenderer ZAxis;
    public LineRenderer DepthAxis;

    public float XScale;
    public float YScale;
    public float ZScale;
    public float DepthScale;

    private void Update()
    {
        if (XAxis.enabled)
            XAxis.SetPositions(new Vector3[] {
                transform.position + -Vector3.right * XScale,
                transform.position + Vector3.right * XScale});

        if (YAxis.enabled)
            YAxis.SetPositions(new Vector3[] {
                transform.position + -Vector3.up * YScale,
                transform.position + Vector3.up * YScale});

        if (ZAxis.enabled)
            ZAxis.SetPositions(new Vector3[] {
                transform.position + -Vector3.forward * ZScale,
                transform.position + Vector3.forward * ZScale});

        if (DepthAxis.enabled)
            DepthAxis.SetPositions(new Vector3[] {
                transform.position + -transform.up * DepthScale,
                transform.position + transform.up * DepthScale});
    }
}
