# YoloDetector

Desktop application for searching objects in images.

![Application](https://i.imgur.com/f9wS4WZ.jpg)

The realization of [YOLO DNNs](https://docs.opencv.org/3.4.1/da/d9d/tutorial_dnn_yolo.html) with [OpenCvSharp](https://github.com/shimat/opencvsharp).

Used [YOLOv2](https://pjreddie.com/darknet/yolov2/) with datasets COCO and VOC.

## Install
To run the app download these files:

| Filename | Download |
| -------- | -------- |
| coco.weights  | https://pjreddie.com/media/files/yolov2.weights |
| cocotiny.weights  | https://pjreddie.com/media/files/yolov2-tiny.weights |
| voc.weights  | https://pjreddie.com/media/files/yolov2-voc.weights |
| voctiny.weights  | https://pjreddie.com/media/files/yolov2-tiny-voc.weights |

rename them in accordance with **Filename** column and copy to {executable_dir}/dataset dir.

## Requirements
* .NET Framework 4.7
* Microsoft Visual C++ 2017 Redistributable Package
