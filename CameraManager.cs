using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Xml;
using Dahua.LConv;
using MVSDK_Net;
using static MVSDK_Net.IMVDefine;
using static MVSDK_Net.MyCamera;

namespace MedicionCamara
{
    public class CameraManager
    {
        private CLC_CSystem system;
        private List<CLC_Camera> devicesList;
        private MyCamera camera;
        private IMV_Frame frame;

        public CameraManager()
        {
            devicesList = new List<CLC_Camera>();
            system = new CLC_CSystem();
        }

        public string getIMVVersion()
        {
            return "Version Machine Viewer SDK " + IMV_GetVersion();
        }

        public List<CLC_Camera> getList()
        {
            return devicesList;
        }

        public void listDevices()
        {
            devicesList = new List<CLC_Camera>();
            system.discovery(ref devicesList, InterfaceType.typeAll);
        }

        public bool cameraIsReady()
        {
            if (camera != null)
            {
                return camera.IMV_IsGrabbing();
            }
            return false;
        }

        public IMV_Frame getLastFrame()
        {
            return frame;
        }

        public string connectCamera(int cameraId)
        {
            camera = new MyCamera();
            int resultCode = camera.IMV_CreateHandle(IMV_ECreateHandleMode.modeByCameraKey, cameraId, devicesList.ElementAt(cameraId).getKey());
            if (resultCode == IMV_OK)
            {
                resultCode = camera.IMV_Open();
                if (resultCode == IMV_OK && camera.IMV_IsOpen())
                {
                    setExposureTime(10000);
                    resultCode = camera.IMV_StartGrabbing();
                    if (resultCode == IMV_OK && camera.IMV_IsGrabbing())
                    {
                        return "Cámara en línea";
                    }
                    else
                    {
                        return "Error " + resultCode.ToString() + " al intentar abrir el grabber";
                    }
                }
                else
                {
                    return "Error " + resultCode.ToString() + " al intentar abrir la cámara";
                }
            }
            else
            {
                return "Error " + resultCode.ToString() + " al crear handle";
            }
        }

        public void setExposureTime(int time)
        {
            string path = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            path += "\\cameraConfig.xml";

            camera.IMV_SaveDeviceCfg(path);
            XmlDocument file = new XmlDocument();
            file.Load(path);
            file.GetElementsByTagName("ExposureTime").Item(0).InnerText = time.ToString();
            file.Save(path);

            IMV_ErrorList errorList = new IMV_ErrorList();
            camera.IMV_LoadDeviceCfg(path, ref errorList);
        }

        public string disconnectCamera()
        {
            if (camera != null)
            {
                int resultCode = camera.IMV_DestroyHandle();
                if (resultCode == IMV_OK)
                {
                    return "Cámara desconectada exitosamente";
                }
                else
                {
                    return "Error " + resultCode.ToString() + " al desconectar la cámara";
                }
            }
            else
            {
                return "Ninguna cámara para desconectar";
            }
        }

        public string takePicture()
        {
            camera.IMV_ClearFrameBuffer();
            frame = new IMV_Frame();
            int resultCode = camera.IMV_GetFrame(ref frame, 2000);
            if (resultCode == IMV_OK)
            {
                return "Fotografía tomada exitosamente";
            }
            else
            {
                return "Error " + resultCode.ToString() + " al intentar crear recuadro";
            }
        }

        public Bitmap getLastPictureAsBitmap() 
        {
            try
            {
                Bitmap bitmap = new Bitmap((int)frame.frameInfo.width, (int)frame.frameInfo.height, (int)frame.frameInfo.width, PixelFormat.Format8bppIndexed, frame.pData);
                ColorPalette palette = bitmap.Palette;
                for (int i = 0; i <= 255; i++)
                {
                    palette.Entries[i] = Color.FromArgb(i, i, i);
                }
                bitmap.Palette = palette;
                return bitmap;
            }
            catch
            {  
                disconnectCamera();
                return null;
            }            
        }
    }
}
