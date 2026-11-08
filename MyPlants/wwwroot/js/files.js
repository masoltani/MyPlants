// Téléchargement d'un fichier généré par l'application (sauvegarde).
window.myPlantsFiles = {
    download: function (fileName, content, mimeType) {
        const blob = new Blob([content], { type: mimeType || "application/json" });
        const url = URL.createObjectURL(blob);

        const link = document.createElement("a");
        link.href = url;
        link.download = fileName;

        document.body.appendChild(link);
        link.click();
        link.remove();

        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }
};