using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SW.Core
{
    public enum E_Dir
    {
        In,
        ForwardLeft,
        ForwardRight,
        BackLeft,
        BackRight,
    }
    public class QuadtreeNode
    {
        public Vector3 center;
        public Vector3 size;
        private QuadtreeNode fl;
        private QuadtreeNode fr;
        private QuadtreeNode bl;
        private QuadtreeNode br;

        private float _nodeSize ;
        public int lodLevel;
        
        public Vector3 Center { get => center; set => center = value; }
        public Vector3 Size => size;

        public QuadtreeNode(Vector3 center, Vector3 size, int lodLevel)
        {
            this.center = center;
            this.size = size;
            this.lodLevel = lodLevel;
        }

        public void DrowGizoms()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(center, center + Vector3.up * 10);
            Gizmos.color = Color.white;
            if (fl == null)
                Gizmos.DrawWireCube(center, size);
            else
            {
                fl.DrowGizoms();
                fr.DrowGizoms();
                bl.DrowGizoms();
                br.DrowGizoms();
            }
        }

        public E_Dir InArea(Vector3 targetPos)
        {
            if (targetPos.x >= center.x + size.x / 2)    // 太右边
            {
                if (targetPos.z > center.z + size.z / 2)    // 太上面
                    return E_Dir.ForwardRight;
                else
                    return E_Dir.BackRight;
            }
            else if (targetPos.x < center.x - size.x / 2) // 太左边
            {
                if (targetPos.z > center.z + size.z / 2)    // 太上面
                    return E_Dir.ForwardLeft;
                else
                    return E_Dir.BackLeft;
            }
            else if (targetPos.z > center.z + size.z / 2)   // 太上面
            {
                if (targetPos.x >= center.x + size.x / 2)    // 太右边
                    return E_Dir.ForwardRight;
                else
                    return E_Dir.ForwardLeft;
            }
            else if (targetPos.z <= center.z - size.z / 2) // 太下面
            {
                if (targetPos.x >= center.x + size.x / 2)    // 太右边
                    return E_Dir.BackRight;
                else
                    return E_Dir.BackLeft;
            }
            else
            {
                return E_Dir.In;
            }
        }

        public QuadtreeNode Positioning(Vector3 targetPos, float minSize)
        {
            if (size.x <= minSize)
            {
                return this;
            }
            else
            {
                if (fl == null)
                    Segmentaion();
                // 正好在边界时,返回左的上的
                if (targetPos.x <= center.x)    // 目标在中心左边
                {
                    if (targetPos.z >= center.z)    // 目标在中心上边
                        return fl.Positioning(targetPos, minSize);
                    else
                        return bl.Positioning(targetPos, minSize);
                }
                else
                {
                    if (targetPos.z >= center.z)    // 目标在中心上边
                        return fr.Positioning(targetPos, minSize);
                    else
                        return br.Positioning(targetPos, minSize);
                }
            }
        }

        public void Segmentaion(QuadtreeNode nfl = null, QuadtreeNode nfr = null, QuadtreeNode nbl = null, QuadtreeNode nbr = null)
        {
            Vector3 oneFourth = size / 4;
            fl = nfl == null ? new QuadtreeNode(center + new Vector3(-oneFourth.x, 0, oneFourth.z), size / 2, lodLevel - 1) : nfl;
            fr = nfr == null ? new QuadtreeNode(center + new Vector3(oneFourth.x, 0, oneFourth.z), size / 2, lodLevel - 1) : nfr;
            bl = nbl == null ? new QuadtreeNode(center + new Vector3(-oneFourth.x, 0, -oneFourth.z), size / 2, lodLevel - 1) : nbl;
            br = nbr == null ? new QuadtreeNode(center + new Vector3(oneFourth.x, 0, -oneFourth.z), size / 2, lodLevel - 1) : nbr;
        }

        public bool CaculateLodNode()
        {
            if (!CanLod())
            {
                mapmgr.Instance.finalNodeList.Add(this);
                return false;
            }
            Segmentaion();
            fl.CaculateLodNode();
            fr.CaculateLodNode();
            bl.CaculateLodNode();
            br.CaculateLodNode();

            return true;
        }
        public bool CanLod()
        {
            if (lodLevel <= 0) return false; //到达最大Lod等级 0~5
            
            float t;
            t = (mapmgr.Instance.lodJudgeSector * size.x) / (DistanceFromCamera() * Camera.main.fieldOfView);
            if (t >= 1)
                return true;
            else
                return false;
        }
        
        public float DistanceFromCamera()
        {
            return Vector3.Distance(center, Camera.main.transform.position);
        }
    }
}