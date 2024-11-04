import asyncio

HOST = '0.0.0.0'
PORT = 5000

async def handle_client(reader, writer):
    addr = writer.get_extra_info('peername')
    print(f"Connected by {addr}")
    
    while True:
        data = await reader.read(1024)
        if not data:
            break

        filename = data.decode().strip()
        print(f"Received filename: {filename}")

        response = "NO THREATS FOUND\n"
        writer.write(response.encode())

        await writer.drain()
    
    print(f"Connection with {addr} closed")

    writer.close()
    await writer.wait_closed()

async def start_server():
    server = await asyncio.start_server(handle_client, HOST, PORT)

    print(f"Mock Antivirus Server listening on {HOST}:{PORT}")
    async with server:
        await server.serve_forever()

if __name__ == "__main__":
    asyncio.run(start_server())
