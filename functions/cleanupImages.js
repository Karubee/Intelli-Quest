const spawn = require("child_process").spawn;

const KEEP_AT_LEAST = 2;
const CONTAINER_REGISTRIES = [
  "gcr.io/<your project name>",
  "eu.gcr.io/<your project name>/gcf/europe-west3"
];

async function go(registry) {
  console.log(`> ${registry}`);
  const images = await command(`gcloud`, [
    "container",
    "images",
    "list",
    `--repository=${registry}`,
    "--format=json",
  ]);
  for (let i = 0; i < images.length; i++) {
    console.log(`    ${images[i].name}`);
    const image = images[i].name;
    let tags = await command(`gcloud`, [
      "container",
      "images",
      "list-tags",
      image,
      "--format=json",
    ]);
    const totalImages = tags.length;
    // do not touch `latest`
    tags = tags.filter(({ tags }) => !tags.find((tag) => tag === "latest"));
    // sorting by date
    tags.sort((a, b) => {
      const d1 = new Date(a.timestamp.datetime);
      const d2 = new Date(b.timestamp.datetime);
      return d2.getTime() - d1.getTime();
    });
    // keeping at least X number of images
    tags = tags.filter((_, i) => i >= KEEP_AT_LEAST);

    console.log(`      For removal: ${tags.length}/${totalImages}`);
    for (let j = 0; j < tags.length; j++) {
      console.log(
        `      Deleting: ${formatImageTimestamp(tags[j])} | ${tags[j].digest}`
      );
      await command("gcloud", [
        "container",
        "images",
        "delete",
        `${image}@${tags[j].digest}`,
        "--format=json",
        "--quiet",
        "--force-delete-tags",
      ]);
    }
  }
}

function command(cmd, args) {
  return new Promise((done, reject) => {
    const ps = spawn(cmd, args);
    let result = "";

    ps.stdout.on("data", (data) => {
      result += data;
    });

    ps.stderr.on("data", (data) => {
      result += data;
    });

    ps.on("close", (code) => {
      if (code !== 0) {
        console.log(`process exited with code ${code}`);
      }
      try {
        done(JSON.parse(result));
      } catch (err) {
        done(result);
      }
    });
  });
}

function formatImageTimestamp(image) {
  const { year, month, day, hour, minute } = image.timestamp;
  return `${year}-${month}-${day} ${hour}:${minute}`;
}

(async function () {
  for (let i = 0; i < CONTAINER_REGISTRIES.length; i++) {
    await go(CONTAINER_REGISTRIES[i]);
  }
})();