/**
 *Copyright(C) 2015 by #COMPANY#
 *All rights reserved.
 *FileName:     #SCRIPTFULLNAME#
 *Author:       #AUTHOR#
 *Version:      #VERSION#
 *UnityVersion：#UNITYVERSION#
 *Date:         #DATE#
 *Description:   
 *History:
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Aliyun.OSS;
using System.Text;
using System.IO;
using Aliyun.OSS.Common;
using System.Threading;
using System;
using System.Text.RegularExpressions;

public class PutObj : MonoBehaviour
{
    public RawImage raw;
    OssClient client;
    Thread thread;
   public  string LocalPath;
   public  string fileName;
    Action putSuccess;
    public float putProcess;
    Action<float> PutWithProcessCallBack=null;
    private bool isUpdateSuccess=false;
    private void Awake()
    {
        client = new OssClient(config.EndPoint, config.AccessKeyId, config.AccessKeySecret);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       if(Input.GetKeyDown(KeyCode.A))
        {
            Texture2D tt = raw.texture as Texture2D;
            byte[] b = tt.EncodeToPNG();
            PutObjWithImg("Aircaraft/666666.png", b);
        }
    }
  
    //图片上传
    void PutObjWithImg(string fileName, byte[] b)
    {
        try
        {
           
            using (Stream stream = new MemoryStream(b))
            {
                ObjectMetadata objectMetadata = new ObjectMetadata();
                objectMetadata.ContentType = "image/jpg";//改变图片上传格式（默认是jpeg,URL链接不能预览图片只能下载）
                client.PutObject(config.Bucket, fileName, stream, objectMetadata);
              
                Debug.Log("OSS上传成功：" );
            }
        }
        catch (OssException e)
        {
            Debug.Log("OSS字符串上传错误：" + e);
        }
        catch (System.Exception e)
        {
            Debug.Log("系统字符串上传错误：" + e);
        }

    }

    //字符串上传
    void PutObjWithStr(string fileName,string text)
    {
        try
        {
            byte[] b = Encoding.UTF8.GetBytes(text);
            using (Stream stream = new MemoryStream(b))
            {
                ObjectMetadata objectMetadata = new ObjectMetadata();
                objectMetadata.ContentType = "image/jpg";
                client.PutObject(config.Bucket,fileName, stream, objectMetadata);
                Debug.Log("OSS字符串上传成功：" + text);
            }
        }
        catch(OssException e)
        {
            Debug.Log("OSS字符串上传错误："+e);
        }
        catch(System.Exception e)
        {
            Debug.Log("系统字符串上传错误：" + e);
        }
      
    }
   
    /// <summary>
    /// 大文件异步上传
    /// </summary>
    /// <param name="LocalPath"></param>
    /// <param name="fileName"></param>
    public void PutObjFromLocalThread(string LocalPath, string fileName,Action action)
    {
        this.LocalPath = LocalPath;
        this.fileName = fileName;
        putSuccess = action;
        thread = new Thread(PutObjThreadFormLocal);
        thread.Start();
    }
   /// <summary>
   /// 本地上传
   /// </summary>
    void PutObjThreadFormLocal()
    {
        try
        {

            client.PutObject(config.Bucket, fileName, LocalPath);
            Debug.Log("OSS本地上传成功：" + LocalPath);
            isUpdateSuccess = true;
            
           
        }
        catch (OssException e)
        {
            Debug.Log("OSS本地上传错误：" + e);
        }
        catch (System.Exception e)
        {
            Debug.Log("系统本地上传错误：" + e);
        }
        finally
        {
            thread.Abort();
        }
    }
    /// <summary>
    /// 上传进度
    /// </summary>
    /// <param name="action"></param>
    /// <param name="LocalPath"></param>
    /// <param name="fileName"></param>
    public void PutObjWithPreocess(Action<float> action, string LocalPath, string fileName)
    {
        PutWithProcessCallBack = action;
        this.LocalPath = LocalPath;
        this.fileName = fileName;
        thread = new Thread(PutObjectProcess);
        thread.Start();
    }
    /// <summary>
    /// 获取上传进度
    /// </summary>
    /// <param name="LocalPath"></param>
    /// <param name="fileName"></param>
    void PutObjectProcess()
    {
        try
        {
            using (var fs = File.Open(LocalPath, FileMode.Open))
            {
                PutObjectRequest putObjectRequest = new PutObjectRequest(config.Bucket, fileName,fs);
                putObjectRequest.StreamTransferProgress += PutStreamProcess;
                client.PutObject(putObjectRequest);
            }
        }
        catch (OssException e)
        {
            Debug.Log("OSS进度的上传错误：" + e);
        }
        catch (Exception e)
        {

            Debug.Log("带有进度的上传错误:"+e);
        }
        finally
        {
            thread.Abort();
        }
    }
    /// <summary>
    /// 处理进度
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void PutStreamProcess(object sender,StreamTransferProgressArgs args)
    {
       putProcess= (args.TransferredBytes * 100 / args.TotalBytes) / 100.0f;
       // PutWithProcessCallBack(putProcess);
    }
    private void FixedUpdate()
    {
        if (PutWithProcessCallBack != null)
        {
            PutWithProcessCallBack(putProcess);
            if (putProcess == 1)
            {
                PutWithProcessCallBack = null;
                putProcess = 0;
            }
        }
        if (isUpdateSuccess)

        {
            putSuccess();
            isUpdateSuccess = false;
        }
    }
    /// <summary>
    /// 删除单个文件
    /// </summary>
    /// <param name="filePatth">OSS中的路径</param>
    public   void DeleObj(string filePatth)
    {
        try
        {
            client.DeleteObject(config.Bucket, filePatth);
            Debug.Log("删除单个成功"+ filePatth);
        
        }
        catch (OssException e)
        {
            Debug.Log("OSS删除单个文件错误：" + e);
        }
        catch (System.Exception e)
        {
            Debug.Log("系统删除单个文件错误：" + e);
        }
        finally
        {
           
        }
    }
    /// <summary>
    /// 删除多个文件
    /// </summary>
    /// <param name="filePatths">OSS中的路径集合</param>
    public void DeleObjs(List<string> filePatths)
    {
        try
        {
            DeleteObjectsRequest deleteObjectsRequest = new DeleteObjectsRequest(config.Bucket, filePatths);
            client.DeleteObjects(deleteObjectsRequest);
            Debug.Log("删除多个文件成功：");

        }
        catch (OssException e)
        {
            Debug.Log("OSS删除多个文件错误：" + e);
        }
        catch (System.Exception e)
        {
            Debug.Log("系统删除多个文件错误：" + e);
        }
        finally
        {
           
        }
    }
    /// <summary>
    /// 获取所有的名字和后缀
    /// </summary>
    /// <returns></returns>
    public List<string> GetAllFileName()
    {
        ObjectListing list = client.ListObjects(config.Bucket);
        List<string> nameList = new List<string>();

        foreach (var item in list.ObjectSummaries)
        {
            if (Regex.IsMatch(item.Key, "/") == false)
            {
                nameList.Add(item.Key);
            }
        }
        return nameList;
    }
    /// <summary>
    /// 创建空文件夹
    /// </summary>
    public void CreatEmptyFloder(string floderName)
    {
        try
        {
            using (var stream = new MemoryStream())
            {
                client.PutObject(config.Bucket, floderName + "/", stream);
            }
        }
        catch (OssException e)
        {
            Debug.Log("创建文件夹出错：" + e.Message);
        }
        catch (System.Exception e)
        {
            Debug.Log("创建文件夹出错：" + e.Message);
        }
    }
}
