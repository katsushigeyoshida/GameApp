using OpenTK;
using System;
using System.Collections.Generic;
using WpfLib;

namespace GameApp
{
    /// <summary>
    /// 整数型3次元ベクトル
    /// </summary>
    class Vector3I
    {
        public int X = 0;
        public int Y = 0;
        public int Z = 0;

        public Vector3I()
        {
        }

        public Vector3I(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3I(float x, float y, float z)
        {
            X = (int)Math.Round(x);
            Y = (int)Math.Round(y);
            Z = (int)Math.Round(z);
        }

        public Vector3I(Vector3I v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }

        public Vector3I(Vector3 vec)
        {
            X = (int)Math.Round(vec.X);
            Y = (int)Math.Round(vec.Y);
            Z = (int)Math.Round(vec.Z);
        }

        public Vector3 get()
        {
            return new Vector3(X, Y, Z);
        }

        /// <summary>
        /// 2つのベクトルの角度(0 <= θ <= PI
        /// 内積の公式　OA・OB = |OA||OB|cos(θ)より求める
        /// </summary>
        /// <param name="vf"></param>
        /// <param name="vs"></param>
        /// <returns>角度(rad)</returns>
        public float getAngle(Vector3 vf, Vector3 vs)
        {
            if (length(vf) * length(vs) == 0f)
                return -1f;
            float cosang = innerProduct(vf, vs) / (length(vf) * length(vs));
            if (1f < cosang)
                cosang = 1f;
            else if (cosang < -1f)
                cosang = -1f;
            return (float)Math.Acos(cosang);
        }

        /// <summary>
        ///  XY平面上での2っのベクトルの角度
        ///  時計回りに0<= θ <= 2PI
        /// </summary>
        /// <param name="vf"></param>
        /// <param name="vs"></param>
        /// <returns></returns>
        public float getAngleXY(Vector3 vf, Vector3 vs)
        {
            Vector3 vf2 = new Vector3(vf);
            vf2.Z = 0f;
            Vector3 vs2 = new Vector3(vs);
            vs2.Z = 0f;
            float ang = getAngle(vf2, vs2);
            Vector3 outProduct = outerProduct(vf2, vs2);
            if (0f > outProduct.Z)
                ang = (float)Math.PI * 2f - ang;
            return ang;
        }

        public float getAngleYZ(Vector3 vf, Vector3 vs)
        {
            Vector3 vf2 = new Vector3(vf);
            vf2.X = 0f;
            Vector3 vs2 = new Vector3(vs);
            vs2.X = 0f;
            float ang = getAngle(vf2, vs2);
            Vector3 outProduct = outerProduct(vf2, vs2);
            if (0f > outProduct.X)
                ang = (float)Math.PI * 2f - ang;
            return ang;
        }

        public float getAngleZX(Vector3 vf, Vector3 vs)
        {
            Vector3 vf2 = new Vector3(vf);
            vf2.Y = 0f;
            Vector3 vs2 = new Vector3(vs);
            vs2.Y = 0f;
            float ang = getAngle(vf2, vs2);
            Vector3 outProduct = outerProduct(vf2, vs2);
            if (0f > outProduct.Y)
                ang = (float)Math.PI * 2f - ang;
            return ang;
        }

        /// <summary>
        /// ベクトルの内積
        /// </summary>
        /// <param name="vf"></param>
        /// <param name="vs"></param>
        /// <returns></returns>
        private float innerProduct(Vector3 vf, Vector3 vs)
        {
            return vf.X * vs.X + vf.Y * vs.Y + vf.Z * vs.Z;
        }

        /// <summary>
        /// ベクトルの外積
        /// </summary>
        /// <param name="vf"></param>
        /// <param name="vs"></param>
        /// <returns></returns>
        private Vector3 outerProduct(Vector3 vf, Vector3 vs)
        {
            return new Vector3(vf.Y * vs.Z - vf.Z * vs.Y, vf.Z * vs.X - vf.X * vs.Z, vf.X * vs.Y - vf.Y * vs.X);
        }

        /// <summary>
        /// ベクトルのXY面での長さ
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        private float length(Vector3 vec)
        {
            return (float)Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
        }


        public static Vector3I operator +(Vector3I left, Vector3I right)
        {
            return new Vector3I(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public static Vector3I operator -(Vector3I left, Vector3I right)
        {
            return new Vector3I(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        public static Vector3I operator -(Vector3I vec)
        {
            return new Vector3I(-vec.X, -vec.Y, -vec.Z);
        }
    }

    /// <summary>
    /// 立方体操作クラス
    /// </summary>
    class CubeUnit
    {
        public int mId = 0;                     //  識別子
        public Vector3 mPos;                    //  元の位置座標
        public Vector3 mTranPos;                //  移動後の位置座標
        public Vector3 mAng;                    //  軸に対する回転角(deg)
        public Vector3I mAngInt;                //  軸に対する回転角(deg)
        public Vector3I mPosInt;                //  元の立方体の位置({x,y,z})
        public Vector3I mTranPosInt;            //  移動後の立方体の位置({x,y,z})
        public List<Vector3I> mAngList;         //  操作リスト

        private int mDigit = 6;
        private YLib ylib = new YLib();

        public CubeUnit()
        {
            mPos = new Vector3();
            mTranPos = new Vector3();
            mAng = new Vector3();
            mPosInt = new Vector3I();
            mTranPosInt = new Vector3I();
            mAngInt = new Vector3I();
            mAngList = new List<Vector3I>();
        }

        public CubeUnit(Vector3 pos)
        {
            mPos = new Vector3(pos);
            mTranPos = new Vector3(mPos);
            mAng = new Vector3();
            mPosInt = new Vector3I(mPos);
            mTranPosInt = new Vector3I(mPos);
            mAngInt = new Vector3I();
            mAngList = new List<Vector3I>();
        }

        public CubeUnit(Vector3 pos, int id)
        {
            mPos = new Vector3(pos);
            mTranPos = new Vector3(mPos);
            mAng = new Vector3();
            mPosInt = new Vector3I(mPos);
            mTranPosInt = new Vector3I(mPos);
            mAngInt = new Vector3I();
            mAngList = new List<Vector3I>();
            mId = id;
        }

        /// <summary>
        /// 前回の軸の回転角から移動位置を求める
        /// </summary>
        /// <param name="xang">X軸で回転(deg)</param>
        /// <param name="yang">Y軸で回転(deg)</param>
        /// <param name="zang">Z軸で回転(deg)</param>
        public void setAddAngle(int xang, int yang, int zang)
        {
            setAddAngle(new Vector3I(xang, yang, zang));
        }

        /// <summary>
        /// 軸の回転角に対して座標位置をもとめる
        /// </summary>
        /// <param name="ang">回転角</param>
        public void setAddAngle(Vector3I ang)
        {
            mAngInt += ang;
            mAngList.Add(ang);
            mTranPos = transPos(mPos, mAngList);   //  初期位置からの移動位置
            normalizeIntPos(mAngInt);              //  90°ごとの位置を求める
        }

        /// <summary>
        /// 座標位置を回転移動させる
        /// </summary>
        /// <param name="pos">3次元座標</param>
        /// <returns></returns>
        private Vector3 transPos(Vector3 pos, List<Vector3I> angList)
        {
            Vector3 outPos;
            outPos = pos;
            for (int i = 0; i < angList.Count; i++) {
                if (0 != angList[i].X)
                    outPos = RotateX(outPos, angList[i].X);
                else if (0 != angList[i].Y)
                    outPos = RotateY(outPos, angList[i].Y);
                else if (0 != angList[i].Z)
                    outPos = RotateZ(outPos, angList[i].Z);
            }
            //outPos = roundVector(outPos);     //  座標値の丸め
            return outPos;
        }

        /// <summary>
        /// 90°おきに整数位置を設定する
        /// </summary>
        /// <param name="ang"></param>
        private void normalizeIntPos(Vector3I ang)
        {
            if (ang.X % 90 == 0 && ang.Y % 90 == 0 && ang.Z % 90 == 0) {
                mTranPosInt = new Vector3I(mTranPos);
                //mTranPos = new Vector3(mTranPosInt.X, mTranPosInt.Y, mTranPosInt.Z);
            }
        }

        /// <summary>
        /// X軸で回転(時計回り)
        /// </summary>
        /// <param name="vec">3次元座標</param>
        /// <param name="ang">回転角(deg)</param>
        /// <returns>変換後の3次元座標</returns>
        public Vector3 RotateX(Vector3 vec, float ang)
        {
            Vector3 outVec = new Vector3();
            float rang = ang * (float)Math.PI / 180f;
            outVec.X = vec.X;
            outVec.Y = vec.Y * (float)Math.Cos(rang) - vec.Z * (float)Math.Sin(rang);
            outVec.Z = vec.Z * (float)Math.Cos(rang) + vec.Y * (float)Math.Sin(rang);
            return outVec;
        }

        /// <summary>
        /// Y軸で回転
        /// </summary>
        /// <param name="vec">3次元座標</param>
        /// <param name="ang">回転角(deg)</param>
        /// <returns>変換後の3次元座標</returns>
        public Vector3 RotateY(Vector3 vec, float ang)
        {
            Vector3 outVec = new Vector3();
            float rang = ang * (float)Math.PI / 180f;
            outVec.X = vec.X * (float)Math.Cos(rang) + vec.Z * (float)Math.Sin(rang);
            outVec.Y = vec.Y;
            outVec.Z = vec.Z * (float)Math.Cos(rang) - vec.X * (float)Math.Sin(rang);
            return outVec;
        }

        /// <summary>
        /// Z軸で回転
        /// </summary>
        /// <param name="vec">3次元座標</param>
        /// <param name="ang">回転角(deg)</param>
        /// <returns>変換後の3次元座標</returns>
        public Vector3 RotateZ(Vector3 vec, float ang)
        {
            Vector3 outVec = new Vector3();
            float rang = ang * (float)Math.PI / 180f;
            outVec.X = vec.X * (float)Math.Cos(rang) - vec.Y * (float)Math.Sin(rang);
            outVec.Y = vec.Y * (float)Math.Cos(rang) + vec.X * (float)Math.Sin(rang);
            outVec.Z = vec.Z;
            return outVec;
        }

        /// <summary>
        /// 三次元データの丸め処理
        /// </summary>
        /// <param name="vec">3次元座標</param>
        /// <returns>丸め後の3次元座標</returns>
        private Vector3 roundVector(Vector3 vec)
        {
            vec.X = ylib.roundRound(vec.X, mDigit);
            vec.Y = ylib.roundRound(vec.Y, mDigit);
            vec.Z = ylib.roundRound(vec.Z, mDigit);
            vec.X = Math.Abs(vec.X) < 1e-5 ? 0f : vec.X;
            vec.Y = Math.Abs(vec.Y) < 1e-5 ? 0f : vec.Y;
            vec.Z = Math.Abs(vec.Z) < 1e-5 ? 0f : vec.Z;
            return vec;
        }
    }
}
