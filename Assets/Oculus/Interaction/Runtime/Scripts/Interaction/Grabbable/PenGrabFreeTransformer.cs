/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// A Transformer that moves the target in a 1-1 fashion with the GrabPoint.
    /// Updates transform the target in such a way as to maintain the target's
    /// local positional and rotational offsets from the GrabPoint.
    /// </summary>
    public class PenGrabFreeTransformer : MonoBehaviour, ITransformer
    {

        private IGrabbable _grabbable;
        private Pose _grabDeltaInLocalSpace;
        private static Pen_3D painter; // 新增Painter引用
        private static Board board; // 新增Painter引用
        public float distance;

        public void Initialize(IGrabbable grabbable)
        {
            _grabbable = grabbable;
            painter = this.GetComponent<Pen_3D>();
            board = painter.board;
        }

        public void BeginTransform()
        {
            Pose grabPoint = _grabbable.GrabPoints[0];
            var targetTransform = _grabbable.Transform;
            _grabDeltaInLocalSpace = new Pose(targetTransform.InverseTransformVector(grabPoint.position - targetTransform.position),
                                            Quaternion.Inverse(grabPoint.rotation) * targetTransform.rotation);
        }
        public void UpdateTransform()
        {
            Pose grabPoint = _grabbable.GrabPoints[0];
            var targetTransform = _grabbable.Transform;
            targetTransform.rotation = grabPoint.rotation * _grabDeltaInLocalSpace.rotation;
            targetTransform.position = grabPoint.position - targetTransform.TransformVector(_grabDeltaInLocalSpace.position);
            if (this.GetComponent<Grabbable>().isGrabbed() == "pen")
            {
                distance = board.GetDistanceFromBoardPlane(painter.tip.position);//笔尖距离平面的距离
                //Debug.Log("xxxxxxx"+distance);
                bool isPositiveOfBoardPlane = board.GetSideOfBoardPlane(painter.tip.position);//笔尖是不是在笔尖的正面
                Vector3 direction = this.transform.position - painter.tip.position;//笔尖位置指向笔的位置的差向量
                //当笔尖穿透的时候，需要矫正笔的位置 
                if (isPositiveOfBoardPlane || distance > 0.0001f)
                {
                    //Debug.Log(isPositiveOfBoardPlane);
                    Vector3 pos = board.ProjectPointOnBoardPlane(painter.tip.position);
                    targetTransform.position = pos - board.boardPlane.normal * 0.001f + direction;//pos是笔尖的位置，而不是笔的位置，加上direction后才是笔的位置 
                }
            }
        }

        public void EndTransform() { }
    }
}
