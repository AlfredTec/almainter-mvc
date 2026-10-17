(function () {
    const input = document.querySelector('[data-foto-input]');
    if (!input) return;

    const vista = document.getElementById('fotoVista');
    const LADO_MAXIMO = 1280;
    const CALIDAD = 0.8;

    input.addEventListener('change', async () => {
        const archivo = input.files[0];
        if (!archivo) return;

        try {
            const imagen = await createImageBitmap(archivo);
            const escala = Math.min(1, LADO_MAXIMO / Math.max(imagen.width, imagen.height));

            const canvas = document.createElement('canvas');
            canvas.width = Math.round(imagen.width * escala);
            canvas.height = Math.round(imagen.height * escala);

            const ctx = canvas.getContext('2d');
            ctx.fillStyle = '#fff'; // fondo blanco para PNG/WebP con transparencia
            ctx.fillRect(0, 0, canvas.width, canvas.height);
            ctx.drawImage(imagen, 0, 0, canvas.width, canvas.height);
            imagen.close();

            const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/jpeg', CALIDAD));
            if (!blob) throw new Error('No se pudo procesar la imagen');

            const nombre = (archivo.name.replace(/\.[^.]+$/, '') || 'foto') + '.jpg';
            const transferencia = new DataTransfer();
            transferencia.items.add(new File([blob], nombre, { type: 'image/jpeg' }));
            input.files = transferencia.files;

            if (vista) {
                vista.src = URL.createObjectURL(blob);
                vista.classList.remove('d-none');
            }
        } catch (error) {
            // Si el navegador no puede procesarla, se envía tal cual: el servidor valida formato y tamaño
            console.warn('No se pudo reducir la foto:', error);
        }
    });
})();