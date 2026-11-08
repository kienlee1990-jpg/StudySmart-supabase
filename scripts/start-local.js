const { spawn } = require("node:child_process")

const root = process.cwd()
const children = new Set()
let stopping = false

function stop(exitCode = 0) {
  if (stopping) return
  stopping = true
  process.exitCode = exitCode

  for (const child of children) {
    if (child.exitCode === null && !child.killed) child.kill("SIGTERM")
  }
}

function start(label, command, args, env = process.env) {
  const child = spawn(command, args, {
    cwd: root,
    env,
    stdio: "inherit",
    shell: process.platform === "win32",
  })
  children.add(child)

  child.on("error", (error) => {
    console.error(`[${label}] Could not start: ${error.message}`)
    stop(1)
  })

  child.on("exit", (code, signal) => {
    children.delete(child)
    if (stopping) return
    console.error(`[${label}] stopped${signal ? ` (${signal})` : ` (exit ${code})`}`)
    stop(code || 1)
  })

  return child
}

process.on("SIGINT", () => stop(130))
process.on("SIGTERM", () => stop(143))

console.log("StudySmart local: http://localhost:5173 (API: http://localhost:5080)")

start("API", "dotnet", [
  "run",
  "--project",
  "API/QuanLyTaiKhoanAPI/QuanLyTaiKhoanAPI.csproj",
  "--no-launch-profile",
], {
  ...process.env,
  ASPNETCORE_ENVIRONMENT: "Development",
  ASPNETCORE_URLS: "http://localhost:5080",
})

start("UI", process.platform === "win32" ? "npm.cmd" : "npm", ["--prefix", "UI", "run", "dev"])
