// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
const connectionStatus = document.getElementById("connectionStatus");
const eyeTrackingStatus = document.getElementById("eyeTrackingStatus");
const screen = document.getElementById("screen");
// Canvas-based drawing
const mainCanvas = document.getElementById("mainCanvas");
const mainCtx = mainCanvas.getContext("2d");
const servicesSelect = document.getElementById("servicesSelect");
const lastTimestamp = document.getElementById("lastTimestamp");
const lastX = document.getElementById("lastX");
// Redraw canvas on window resize and tracker change
window.addEventListener('resize', () => {
  // Optionally, resize canvas to match screen div if you want responsive
  // For now, just redraw
  drawScene();
});
servicesSelect.addEventListener('change', () => {
  drawScene();
});

const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hub")
  .configureLogging(signalR.LogLevel.Information)
  .build();

// Also, redraw on connection start to clear any old axes
connection.start()
  .then(() => {
    connectionStatus.textContent = "Connected";
    drawScene();
    return connection.invoke("GetTrackerServices");
  })
  .catch((err) => {
    connectionStatus.textContent = "Disconnected";
    console.error(err.toString());
  });

const lastY = document.getElementById("lastY");
const headLastTimestamp = document.getElementById("headLastTimestamp");
const headX = document.getElementById("headX");
const headY = document.getElementById("headY");
const headZ = document.getElementById("headZ");
const headPitch = document.getElementById("headPitch");
const headYaw = document.getElementById("headYaw");
const headRoll = document.getElementById("headRoll");

// Track the largest absolute head X and Y values seen for normalization
let maxAbsHeadX = 1;
let maxAbsHeadY = 1;

// Store last gaze and head data for drawing
let lastGaze = { x: null, y: null };
let lastHead = { x: null, y: null, z: null, pitch: null, yaw: null, roll: null };

function drawScene() {
  // Clear canvas
  mainCtx.clearRect(0, 0, mainCanvas.width, mainCanvas.height);

  // Draw gaze dot (red)
  if (lastGaze.x !== null && lastGaze.y !== null) {
    const cx = mainCanvas.width / 2 + (mainCanvas.width / 2) * lastGaze.x;
    const cy = mainCanvas.height / 2 - (mainCanvas.height / 2) * lastGaze.y;
    mainCtx.beginPath();
    mainCtx.arc(cx, cy, 5, 0, 2 * Math.PI);
    mainCtx.fillStyle = "red";
    mainCtx.fill();
  }

  // Draw head dot (blue) and axes
  if (lastHead.x !== null && lastHead.y !== null && lastHead.z !== null) {
    // Normalize x/y to [-1, 1] based on largest seen
    let normX = maxAbsHeadX !== 0 ? lastHead.x / maxAbsHeadX : 0;
    let normY = maxAbsHeadY !== 0 ? lastHead.y / maxAbsHeadY : 0;
    const cx = mainCanvas.width / 2 + (mainCanvas.width / 2) * normX;
    const cy = mainCanvas.height / 2 - (mainCanvas.height / 2) * normY;
    // Blue dot
    mainCtx.beginPath();
    mainCtx.arc(cx, cy, 6, 0, 2 * Math.PI);
    mainCtx.fillStyle = "rgba(0,0,255,0.7)";
    mainCtx.fill();

    // Draw axes if rotation is available
    if (lastHead.pitch !== null && lastHead.yaw !== null && lastHead.roll !== null) {
      // Invert pitch, yaw, roll for mirror effect
      let pitchRad = -lastHead.pitch * Math.PI / 180;
      let yawRad = -lastHead.yaw * Math.PI / 180;
      let rollRad = -lastHead.roll * Math.PI / 180;
      function rotate(v) {
        let [x, y, z] = v;
        // Roll (Z)
        let x1 = x * Math.cos(rollRad) - y * Math.sin(rollRad);
        let y1 = x * Math.sin(rollRad) + y * Math.cos(rollRad);
        let z1 = z;
        // Pitch (X)
        let x2 = x1;
        let y2 = y1 * Math.cos(pitchRad) - z1 * Math.sin(pitchRad);
        let z2 = y1 * Math.sin(pitchRad) + z1 * Math.cos(pitchRad);
        // Yaw (Y)
        let x3 = x2 * Math.cos(yawRad) + z2 * Math.sin(yawRad);
        let y3 = y2;
        let z3 = -x2 * Math.sin(yawRad) + z2 * Math.cos(yawRad);
        return [x3, y3, z3];
      }
      const axes = [
        { v: [1, 0, 0], color: "red" },    // right
        { v: [0, 1, 0], color: "green" },  // up
        { v: [0, 0, 1], color: "blue" },   // forward
      ];
      const axisLength = 40;
      axes.forEach(axis => {
        const [dx, dy, dz] = rotate(axis.v);
        mainCtx.beginPath();
        mainCtx.moveTo(cx, cy);
        mainCtx.lineTo(cx + dx * axisLength, cy - dy * axisLength);
        mainCtx.strokeStyle = axis.color;
        mainCtx.lineWidth = 3;
        mainCtx.stroke();
      });
    }
  }
}

// ...existing code...


// Handle Gaze updates
connection.on("TrackerGazeUpdate", (name, timestamp, x, y) => {
  if (name != servicesSelect.value) {
    return;
  }
  if (x !== null && y !== null) {
    lastX.textContent = x.toFixed(2);
    lastY.textContent = y.toFixed(2);
    eyeTrackingStatus.textContent = "Tracking";
    lastGaze.x = x;
    lastGaze.y = y;
  } else {
    lastGaze.x = null;
    lastGaze.y = null;
  }
  drawScene();
  if (timestamp !== null && lastTimestamp) {
    let ts = Number(timestamp);
    if (!isNaN(ts) && ts > 1000000000000) {
      let ms = (ts - 621355968000000000) / 10000;
      let date = new Date(ms);
      lastTimestamp.textContent = date.toLocaleString();
    } else {
      lastTimestamp.textContent = timestamp;
    }
  }
});

// Handle Head updates
connection.on("TrackerHeadUpdate", (name, timestamp, x, y, z, pitch, yaw, roll) => {
  if (name != servicesSelect.value) {
    return;
  }
  if (x !== null && y !== null && z !== null) {
    headX.textContent = x.toFixed(2);
    headY.textContent = y.toFixed(2);
    headZ.textContent = z.toFixed(2);
    if (Math.abs(x) > maxAbsHeadX) maxAbsHeadX = Math.abs(x);
    if (Math.abs(y) > maxAbsHeadY) maxAbsHeadY = Math.abs(y);
    lastHead.x = x;
    lastHead.y = y;
    lastHead.z = z;
  } else {
    headX.textContent = headY.textContent = headZ.textContent = "N/A";
    lastHead.x = lastHead.y = lastHead.z = null;
  }
  if (pitch !== null && yaw !== null && roll !== null) {
    headPitch.textContent = pitch.toFixed(2);
    headYaw.textContent = yaw.toFixed(2);
    headRoll.textContent = roll.toFixed(2);
    lastHead.pitch = pitch;
    lastHead.yaw = yaw;
    lastHead.roll = roll;
  } else {
    headPitch.textContent = headYaw.textContent = headRoll.textContent = "N/A";
    lastHead.pitch = lastHead.yaw = lastHead.roll = null;
  }
  drawScene();
  if (timestamp !== null && headLastTimestamp) {
    let ts = Number(timestamp);
    if (!isNaN(ts) && ts > 1000000000000) {
      let ms = (ts - 621355968000000000) / 10000;
      let date = new Date(ms);
      headLastTimestamp.textContent = date.toLocaleString();
    } else {
      headLastTimestamp.textContent = timestamp;
    }
  }
});

connection.on("TrackerServices", (services) => {
  console.info("Tracker services", services);
  servicesSelect.innerHTML = "";
  services.forEach((service) => {
    const option = document.createElement("option");
    option.text = service.name;
    option.value = service.fullName;
    servicesSelect.add(option);
  });
});

connection.on("TrackingStarted", () => {
  console.info("Tracking started");
  eyeTrackingStatus.textContent = "Tracking";
});

connection.on("TrackingStopped", (name) => {
  console.info("Tracking stopped: " + name);
  eyeTrackingStatus.textContent = "Not Tracking";
});

// ...existing code...

startEyeTracking = () => {
  let name = servicesSelect.value;
  console.log("Start tracking: " + name);
  connection.invoke("StartTracking", name).catch((err) => {
    console.error(err.toString());
  });
};

stopEyeTracking = () => {
  let name = servicesSelect.value;
  console.log("Stop tracking: " + name);
  connection.invoke("StopTracking", name).catch((err) => {
    console.error(err.toString());
  });
};
