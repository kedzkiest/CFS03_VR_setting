using UnityEngine;
using Mocopi.Receiver;

public class RobotArmAvatar : MocopiAvatarBase
{
    [Header("Robot joints")]
    public ArticulationBody link1;   // Yaw  (Y)
    public ArticulationBody link2;   // Pitch (X)
    public ArticulationBody link3;   // Pitch (X)
    public ArticulationBody link4;   // Pitch (X)
    public ArticulationBody link5;   // Yaw  (Y)
    public ArticulationBody link6;   // Roll (Z)

    /* 受信バッファ */
    volatile float _l1, _l2, _l3, _l4, _l5, _l6;

    /* RH → LH */
    static Quaternion RH2LH(float x,float y,float z,float w)
        => new Quaternion(-x,-y, z,-w);

    int[] _idx = new int[27];

    /* ---- Skeleton ---- */
    public override void InitializeSkeleton(
        int[] boneId, int[] parent,
        float[] rx,float[] ry,float[] rz,float[] rw,
        float[] px,float[] py,float[] pz)
    {
        base.InitializeSkeleton(boneId,parent,rx,ry,rz,rw,px,py,pz);
        for(int i=0;i<boneId.Length;i++) _idx[boneId[i]] = i;
    }

    /* ---- UDP 受信スレッド ---- */
    // https://www.sony.co.jp/en/Products/mocopi-dev/jp/documents/Home/TechSpec.html
    public override void UpdateSkeleton(
        int frame,float ts,double unix,
        int[] bid,
        float[] rx,float[] ry,float[] rz,float[] rw,
        float[] px,float[] py,float[] pz)
    {
        /* Shoulder (ID 16) */     // 7/28: IDを変更
        var eS = RH2LH(rx[_idx[16]], ry[_idx[16]], rz[_idx[16]], rw[_idx[16]]).eulerAngles;
        _l1 = Wrap(eS.y); // link1 Yaw (Y-axis)
        _l2 = Wrap(eS.x); // link2 Pitch (X-axis)
        _l6 = Wrap(eS.z); // link6 Roll (Z-axis)

        /* Elbow (ID 16) */
        var eE = RH2LH(rx[_idx[17]], ry[_idx[17]], rz[_idx[17]], rw[_idx[17]]).eulerAngles;
        _l3 = Wrap(eE.x); // link3 Pitch (X-axis)

        /* Wrist (ID 17) */
        var eW = RH2LH(rx[_idx[18]], ry[_idx[18]], rz[_idx[18]], rw[_idx[18]]).eulerAngles;
        _l4 = Wrap(eW.x); // link4 Pitch (X-axis)
        _l5 = Wrap(eW.y); // link5 Yaw (Y-axis)
    }

    /* ---- 物理ステップ ---- */
    void FixedUpdate()
    {
        Apply(link1,_l1);   // Yaw
        Apply(link2,_l2);   // Pitch
        Apply(link3,_l3);   // Pitch
        Apply(link4,_l4);   // Pitch
        Apply(link5,_l5);   // Yaw
        Apply(link6,_l6);   // Roll
    }

    /* ---- Utility ---- */
    static float Wrap(float deg) => Mathf.DeltaAngle(0,deg);

    static void Apply(ArticulationBody j,float targetDeg)
    {
        var d = j.xDrive;
        d.target = targetDeg;
        j.xDrive = d;
    }
}
