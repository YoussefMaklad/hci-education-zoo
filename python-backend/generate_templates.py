import os
import cv2
import mediapipe as mp

# Initialize MediaPipe Hands and Drawing utilities
mp_drawing = mp.solutions.drawing_utils
mp_hands = mp.solutions.hands

hands = mp_hands.Hands(
    static_image_mode=False,
    max_num_hands=1,
    min_detection_confidence=0.3,
    min_tracking_confidence=0.3
)

def loop_files(directory):
    f = open(directory + "result_generated_templates.py", "w")
    f.write("from dollarpy import Recognizer, Template, Point\n")
    recstring = ""
    for file_name in os.listdir(directory):
        if os.path.isfile(os.path.join(directory, file_name)):
            if file_name.endswith(".mp4"):
                print(file_name)
                foo = file_name[:-4]
                recstring += foo + ","
                f.write(f"{foo} = Template('{foo}', [\n")

                camera = cv2.VideoCapture(os.path.join(directory, file_name))
                framecnt = 0
                while camera.isOpened():
                    ret, frame = camera.read()
                    if not ret:
                        print("Can't receive frame (stream end?). Exiting ...")
                        break

                    frame = cv2.resize(frame, (480, 320))
                    framecnt += 1

                    # Convert the frame to RGB format
                    rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                    print(framecnt)

                    # Process the RGB frame to get the hand landmarks
                    results = hands.process(rgb_frame)
                    image_height, image_width, _ = frame.shape

                    if results.multi_hand_landmarks:
                        for hand_landmarks in results.multi_hand_landmarks:
                            try:
                                # Extracting the positions of key landmarks (e.g., WRIST)
                                x = str(int(hand_landmarks.landmark[mp_hands.HandLandmark.WRIST].x * image_width))
                                y = str(int(hand_landmarks.landmark[mp_hands.HandLandmark.WRIST].y * image_height))
                                f.write(f"Point({x}, {y}, 1),\n")

                                # Optionally, extract more landmarks here (e.g., THUMB_TIP, INDEX_FINGER_TIP)
                                x = str(int(hand_landmarks.landmark[mp_hands.HandLandmark.INDEX_FINGER_TIP].x * image_width))
                                y = str(int(hand_landmarks.landmark[mp_hands.HandLandmark.INDEX_FINGER_TIP].y * image_height))
                                f.write(f"Point({x}, {y}, 1),\n")

                                # Draw hand landmarks on the frame
                                mp_drawing.draw_landmarks(frame, hand_landmarks, mp_hands.HAND_CONNECTIONS)
                            except Exception as e:
                                print(f"An error occurred when processing file: {file_name}, error: {e}")

                    # Show the output frame
                    cv2.imshow('Output', frame)

                    if cv2.waitKey(1) == ord('q'):
                        break

                f.write("])\n")
                camera.release()
                cv2.destroyAllWindows()

    recstring = recstring[:-1]
    f.write ("recognizer = Recognizer(["+recstring+"])\n")    
    f.close()

directory_path = "D:\\HCI Project\\hci-education-zoo\\python-backend\\New_Videos_For_Templates\\"
loop_files(directory_path)