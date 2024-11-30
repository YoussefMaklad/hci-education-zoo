import cv2
import mediapipe as mp
from dollarpy import Recognizer, Template, Point
from result_generated_templates import recognizer

# Initialize MediaPipe Hands and Drawing utilities
mp_drawing = mp.solutions.drawing_utils
mp_hands = mp.solutions.hands

hands = mp_hands.Hands(
    static_image_mode=False,
    max_num_hands=1,
    min_detection_confidence=0.3,
    min_tracking_confidence=0.3
)

def get_templates():
    return recognizer.templates

def start_test():
    Allpoints = []
    cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)
    framecnt = 0

    while cap.isOpened():
        ret, frame = cap.read()
        if not ret:
            print("Can't receive frame (stream end?). Exiting ...")
            break
        frame = cv2.resize(frame, (480, 320))
        framecnt += 1
        try:
            # Convert the frame to RGB format
            rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

            # Process the RGB frame to get hand landmarks
            results = hands.process(rgb_frame)

            if results.multi_hand_landmarks:
                for hand_landmarks in results.multi_hand_landmarks:
                    # Get image dimensions
                    image_height, image_width, _ = frame.shape

                    # Add wrist position
                    x = int(hand_landmarks.landmark[mp_hands.HandLandmark.WRIST].x * image_width)
                    y = int(hand_landmarks.landmark[mp_hands.HandLandmark.WRIST].y * image_height)
                    Allpoints.append(Point(x, y, 1))

                    # Add index finger tip position
                    x = int(hand_landmarks.landmark[mp_hands.HandLandmark.INDEX_FINGER_TIP].x * image_width)
                    y = int(hand_landmarks.landmark[mp_hands.HandLandmark.INDEX_FINGER_TIP].y * image_height)
                    Allpoints.append(Point(x, y, 1))

                    # Draw hand landmarks on the frame
                    mp_drawing.draw_landmarks(frame, hand_landmarks, mp_hands.HAND_CONNECTIONS)

            # Perform gesture recognition every 23 frames
            if framecnt % 80 == 0:
                framecnt = 0
                result = recognizer.recognize(Allpoints)
                print(result)
                Allpoints.clear()

            cv2.imshow('Output', frame)

        except Exception as e:
            print(f"Error: {e}")

        if cv2.waitKey(1) == ord('q'):
            break

    cap.release()
    cv2.destroyAllWindows()

templates = get_templates()
start_test()