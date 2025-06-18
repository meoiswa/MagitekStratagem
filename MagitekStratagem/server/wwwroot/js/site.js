// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
const connectionStatus = document.getElementById("connectionStatus");
const eyeTrackingStatus = document.getElementById("eyeTrackingStatus");
const screen = document.getElementById("screen");
const eyePosition = document.getElementById("eyePosition");
const servicesSelect = document.getElementById("servicesSelect");
const lastTimestamp = document.getElementById("lastTimestamp");
const lastX = document.getElementById("lastX");
const lastY = document.getElementById("lastY");

const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hub")
  .configureLogging(signalR.LogLevel.Information)
  .build();

connection.on("TrackerUpdate", (name, timestamp, eyeX, eyeY) => {
  if (name != servicesSelect.value) {
    return;
  }
  if (eyeX !== null && eyeY !== null) {
    lastX.textContent = eyeX.toFixed(2);
    lastY.textContent = eyeY.toFixed(2);

    eyeTrackingStatus.textContent = "Tracking";
    eyePosition.style.display = "block";
    eyePosition.style.left = `${
      -5 + screen.clientWidth / 2 + (screen.clientWidth / 2) * eyeX
    }px`;
    eyePosition.style.top = `${
      -5 + screen.clientHeight / 2 - (screen.clientHeight / 2) * eyeY
    }px`;
  } else {
    eyePosition.style.display = "none";
  }
  if (timestamp !== null && lastTimestamp) {
    // If timestamp is ticks, convert to readable date
    let ts = Number(timestamp);
    if (!isNaN(ts) && ts > 1000000000000) {
      // .NET ticks (100-nanosecond intervals since 1/1/0001)
      // Convert to JS Date: (ticks - 621355968000000000) / 10000
      let ms = (ts - 621355968000000000) / 10000;
      let date = new Date(ms);
      lastTimestamp.textContent = date.toLocaleString();
    } else {
      lastTimestamp.textContent = timestamp;
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

connection
  .start()
  .then(() => {
    connectionStatus.textContent = "Connected";
    return connection.invoke("GetTrackerServices");
  })
  .catch((err) => {
    connectionStatus.textContent = "Disconnected";
    console.error(err.toString());
  });

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
