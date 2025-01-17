﻿using UnityEngine;
using UnityEngine.UI;
using MathNet.Numerics.LinearAlgebra.Single;

namespace NumericIK
{
    public class IKManager : MonoBehaviour
    {
        // slider
        private GameObject[] slider = new GameObject[6];
        private GameObject[] text = new GameObject[6];
        private float[] sliderVal = new float[6];
        private float[] prevSliderVal = new float[6];

        // robot
        public GameObject[] Joint = new GameObject[6];      // joints of arm robot
        private float[] angle = new float[6];               // local rotation around its axis
        private float[] prevAngle = new float[6];           // previous angle used when new angles are not valid
        private Vector3[] dim = new Vector3[7];             // local dimensions of each joint
        private Vector3[] point = new Vector3[7];           // world position of joint end
        private Vector3[] axis = new Vector3[6];            // local direction of each axis
        private Quaternion[] rotation = new Quaternion[6];  // local rotation(quaternion) of joint relative to its parent
        private Quaternion[] wRotation = new Quaternion[6]; // world rotation(quaternion) of joint
        private Vector3 PRef;                                // reference(target) position of hand
        private Vector3 RRef;                                // reference(target) pose of hand

        private float lambda = 0.1f;                        // convergence rate

        private float[] minAngle = new float[6];            // limits of joint rotatation
        private float[] maxAngle = new float[6];

        void Start()
        {
            // slider settings
            for(int i=0; i< Joint.Length; i++)
            {
                slider[i] = GameObject.Find("Slider_"+i.ToString());
                text[i] = GameObject.Find("Text_" + i.ToString());
                sliderVal[i] = slider[i].GetComponent<Slider>().value;
            }

            // limits of joint rotation
            for (int i = 0; i < Joint.Length; i++) // You can set different values for each joint.
            {
                minAngle[i] = -150f;
                maxAngle[i] = 150f;
            }

            // local dimensions of each joint at initial pose
            dim[0] = new Vector3(0f, 2f, 0f);
            dim[1] = new Vector3(0f, 2f, 0f);
            dim[2] = new Vector3(0f, 6f, 0f);
            dim[3] = new Vector3(-0.5f, 3f, 0f);    // offset in x direction 
            dim[4] = new Vector3(0f, 4f, 0f);
            dim[5] = new Vector3(0f, 3f, 0f);
            dim[6] = new Vector3(0f, 1f, 0f);

            // local direction of each axis
            axis[0] = new Vector3(0f, 1f, 0f);
            axis[1] = new Vector3(0f, 0f, 1f);
            axis[2] = new Vector3(0f, 0f, 1f);
            axis[3] = new Vector3(0f, 1f, 0f);
            axis[4] = new Vector3(0f, 0f, 1f);
            axis[5] = new Vector3(0f, 1f, 0f);

            // local initial rotation around axis
            angle[0] = prevAngle[0] = 0f;
            angle[1] = prevAngle[1] = 0f;
            angle[2] = prevAngle[2] = -90f;    // bend elbow
            angle[3] = prevAngle[3] = 0f;
            angle[4] = prevAngle[4] = 0f;
            angle[5] = prevAngle[5] = 0f;
        }

        private void FixedUpdate()  // every 0.1sec
        {
            // set reference
            for (int i = 0; i < Joint.Length; i++)
            {
                prevSliderVal[i] = sliderVal[i];
                sliderVal[i] = slider[i].GetComponent<Slider>().value;
                text[i].GetComponent<Text>().text = sliderVal[i].ToString();
            }
            PRef.x = sliderVal[0];
            PRef.y = sliderVal[1];
            PRef.z = sliderVal[2];
            RRef.x = sliderVal[3];
            RRef.y = sliderVal[4];
            RRef.z = sliderVal[5];
            
            // IK
            CalcIK();
        }

        void CalcIK()
        {
            int count = 0;
            bool outOfLimit = false;    // true when calculated angle is not valid

            for(int i=0; i<6; i++)
            {
                prevAngle[i] = angle[i];
            }
            Debug.Log("angle= " + angle[0] + " " + angle[1] + " " + angle[2] + " " + angle[3] + "_" + angle[4] + "_" + angle[5]);
            
            for (int i = 0; i < 100; i++)   // iteration
            {
                count = i;
                // find position/pose of hand
                ForwardKinematics();

                // create jacobian
                var J = CalcJacobian(); // 6x6 matrix

                // calculate position/pose error from reference
                var err = CalcErr();    // 6x1 matrix(vector)
                float err_norm = (float)err.L2Norm();
                if (err_norm < 1E-3)    // close enough to target
                {
                    for (int ii = 0; ii < 6; ii++)
                    {
                        if (angle[ii] < minAngle[ii] || angle[ii] > maxAngle[ii])
                        {
                            outOfLimit = true;
                            break;
                        }
                    }
                    break;
                }

                // correct angle of joionts
                var dAngle = lambda * J.PseudoInverse() * err; // 6x1 matrix
                for(int ii = 0; ii < Joint.Length; ii++)
                {
                    angle[ii] += dAngle[ii, 0] * Mathf.Rad2Deg;
                }
                if (outOfLimit) break;
            }
            
            if (count == 99 || outOfLimit)  // did not converge or angle out of limit
            {
                for (int i = 0; i < Joint.Length; i++) // reset slider
                {
                    sliderVal[i] = prevSliderVal[i];
                    slider[i].GetComponent<Slider>().value = sliderVal[i];
                    text[i].GetComponent<Text>().text = sliderVal[i].ToString();

                    angle[i] = prevAngle[i];
                }
            }
            else // draw new robot
            {
                for (int i = 0; i < Joint.Length; i++)
                {
                    rotation[i] = Quaternion.AngleAxis(angle[i], axis[i]);
                    Joint[i].transform.localRotation = rotation[i];
                }
            }
        }
        void ForwardKinematics()
        {
            point[0] = dim[0];
            wRotation[0] = Quaternion.AngleAxis(angle[0], axis[0]);
            for (int i = 1; i < Joint.Length; i++)
            {
                point[i] = wRotation[i - 1] * dim[i] + point[i - 1];    // update endpoint position of arm
                rotation[i] = Quaternion.AngleAxis(angle[i], axis[i]);  // new local rotation
                wRotation[i] = wRotation[i - 1] * rotation[i];          // update rotation of joint
            }
            point[Joint.Length] = wRotation[Joint.Length - 1] * dim[Joint.Length] + point[Joint.Length - 1];
        }

        DenseMatrix CalcJacobian()  // differential relation between joint angles and position/rotation of end effector 
        {
            Vector3 w0 = wRotation[0] * axis[0];
            Vector3 w1 = wRotation[1] * axis[1];
            Vector3 w2 = wRotation[2] * axis[2];
            Vector3 w3 = wRotation[3] * axis[3];
            Vector3 w4 = wRotation[4] * axis[4];
            Vector3 w5 = wRotation[5] * axis[5];
            Vector3 p0 = Vector3.Cross(w0, point[6] - point[0]);
            Vector3 p1 = Vector3.Cross(w1, point[6] - point[1]);
            Vector3 p2 = Vector3.Cross(w2, point[6] - point[2]);
            Vector3 p3 = Vector3.Cross(w3, point[6] - point[3]);
            Vector3 p4 = Vector3.Cross(w4, point[6] - point[4]);
            Vector3 p5 = Vector3.Cross(w5, point[6] - point[5]);

            var J = DenseMatrix.OfArray(new float[,]    // using Math.Net
            {
                { p0.x, p1.x, p2.x, p3.x, p4.x, p5.x },
                { p0.y, p1.y, p2.y, p3.y, p4.y, p5.y },
                { p0.z, p1.z, p2.z, p3.z, p4.z, p5.z },
                { w0.x, w1.x, w2.x, w3.x, w4.x, w5.x  },
                { w0.y, w1.y, w2.y, w3.y, w4.y, w5.y  },
                { w0.z, w1.z, w2.z, w3.z, w4.z, w5.z  }
            });
            return J;
        }

        DenseMatrix CalcErr()
        {
            // position error
            Vector3 perr = PRef - point[6];
            // pose error
            Quaternion rerr = Quaternion.Euler(RRef) * Quaternion.Inverse(wRotation[5]);
            // make error vector
            Vector3 rerrVal = new Vector3(rerr.eulerAngles.x, rerr.eulerAngles.y, rerr.eulerAngles.z);
            if (rerrVal.x > 180f) rerrVal.x -= 360f;
            if (rerrVal.y > 180f) rerrVal.y -= 360f;
            if (rerrVal.z > 180f) rerrVal.z -= 360f;
            var err = DenseMatrix.OfArray(new float[,]
            {
                { perr.x },
                { perr.y },
                { perr.z },
                { rerrVal.x * Mathf.Deg2Rad},
                { rerrVal.y * Mathf.Deg2Rad},
                { rerrVal.z * Mathf.Deg2Rad}
            });
            Debug.Log("err= "+ perr.x + " " + perr.y + " " + perr.z + " " + rerrVal.x + "_" + rerrVal.y + "_" + rerrVal.z);
            return err;
        }
    }
}
