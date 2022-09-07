const { Message, NetworkId, Schema } = require("./ubiq");


Schema.add({
    id: "/ubiq.auramanager.servermessage",
    type: "object",
    properties: {
        type: {"type": "string"},
        args: {"type": "string"}
    },
    required: ["type","args"]
});

Schema.add({
    id: "/ubiq.auramanager.spreadBySphereargs",
    type: "object",
    properties: {
        senderId: {$ref: "/ubiq.messaging.networkid"},
        origin: {$ref: "/ubiq.auramanager.cellCoordinate"},
        radius: {"type": "integer"},
        medium: {"type": "string"},
        message: {"type": "string"},
    },
    required: ["senderId","origin","radius","medium","message"]
});

Schema.add({
    id: "/ubiq.auramanager.cellCoordinate",
    type: "object",
    properties: {
        x: {"type": "integer"},
        y: {"type": "integer"},
        z: {"type": "integer"},
    },
    required: ["x", "y", "z"]
});

class AuraCell{
    constructor(cellId){
        this.mediums = new Map(); // Key-MediumName, Value-Set<InterestPeer>
        this.cellId = cellId;
    }

    addClient(medium, clientId){
        if (! this.mediums.has(medium)){
            this.mediums.get(medium).add(clientId);
        }else{
            this.mediums.set(medium, new Set([clientId]));
        }
    }

    getClients(medium){
        if (this.mediums.has(medium)){
            return this.mediums.get(medium);
        }
    }

    removeSingleClient(medium, clientId){
        this.mediums.get(medium).delete(clientId);
    }
}

class ClientFocus{
    constructor(clientId){
        this.mediums = new Map(); // Key-MediumName, AuraCell
        this.clientId = clientId;
    }

    addAuraCell(medium, cellId){
        if (! this.mediums.has(medium)){
            this.mediums.get(medium).add(cellId);
        }else{
            this.mediums.set(medium, new Set([cellId]));
        }
    }

    getAuraCells(medium){
        if (this.mediums.has(medium)){
            return this.mediums.get(medium);
        }
    }

    clearFocus(medium){
        this.mediums.get(medium).clear();
    }
}

class AuraCellDatabase{
    constructor() {
        this.auraCells = new Map(); // AuraCellId, AuraCell
        this.focusingClients = new Map(); // networkId, AuraCell
    }

    addClient(auraCellId, medium, clientId){
        if (! this.auraCells.has(auraCellId)){
            this.auraCells.set(auraCellId, new AuraCell(auraCellId));
        }
        this.auraCells.get(auraCellId).addClient(medium, clientId);

        if (! this.focusingClients.has(clientId)){
            this.focusingClients.set(clientId, new ClientFocus(clientId));
        }
        this.focusingClients.get(clientId).addAuraCell(medium, auraCellId);
    }

    getClients(auraCellId, medium){
        if (this.auraCells.has(auraCellId)){
            return this.auraCells.get(auraCellId).getClients(medium);
        }
        return undefined;
    }

    clearClientFocus(medium, clientId){
        if (this.focusingClients.has(clientId)){
            let focusedCells = this.focusingClients.get(clientId).getAuraCells(medium);
            focusedCells.forEach(cell => {
                if (! this.auraCells.has(cell.cellId)){
                    this.auraCells.get(cell.cellId).removeSingleClient(medium, clientId);
                }
            });
            this.focusingClients.get(clientId).clear();
        }
    }
}

class AuraManager{
    constructor(room){
        this.objectId = new NetworkId(3993);
        this.room = room;
        this.auracellDatabase = new AuraCellDatabase();
    }

    processMessage(source, message){
        try {
            message.object = message.toObject();
        } catch {
            console.log("Aura Client: Invalid JSON in message");
            return;
        }

        if(!Schema.validate(message.object, "/ubiq.auramanager.servermessage", this.onValidationFailure)){
            console.log("Aura Client: message validation failed");
            return;
        }

        message.type = message.object.type;

        if(message.object.args){
            try {
                message.args = JSON.parse(message.object.args);
            } catch {
                console.log("Aura Client: Invalid JSON in message args");
                return;
            }
        }

        switch(message.type){
            case "SpreadMessage":
                if (Schema.validate(message.args, "/ubiq.auramanager.spreadBySphereargs", this.onValidationFailure)) {
                    // Calculate cells to retrieve
                    let origin = message.args.origin;
                    let radius = message.args.radius;

                    let receiverClients = new Set();
                    for (let i = origin.x - radius; i <= origin.x + radius; i++){
                        for (let j = origin.z - radius; j <= origin.z + radius; j ++){
                            let cellId = this.coordToAuraCellId(i, 0, j); // Assumed using Hex Cell here
                            receiverClients.add(this.auracellDatabase.getClients(cellId, message.args.medium));
                        }
                    }

                    this.room.peers.forEach(peer =>{
                        if (peer !== source){
                            if(receiverClients.has(peer.objectId)){
                                peer.send(Message.Create(message.args.senderId, message.args.message));
                                console.log(message.args.message);
                            }
                            peer.send(Message.Create(message.args.senderId, message.args.message));
                        }
                    })
                }
                break;

            case "SphereFocus":
                let origin = message.args.origin;
                let radius = message.args.radius;
                let medium = message.args.medium;
                let clientId = source.objectId;

                this.auracellDatabase.clearClientFocus(medium, clientId);
                for (let i = origin.x - radius; i <= origin.x + radius; i++){
                    for (let j = origin.z - radius; j <= origin.z + radius; j ++){
                        let cellId = this.coordToAuraCellId(i, 0, j); // Assumed using Hex Cell here
                            this.auracellDatabase.addClient(cellId, medium, clientId);
                    }
                }
                break;
        }
    }

    coordToAuraCellId(x, y, z){
        return x.toString() + "," + y.toString() + "," + z.toString();
    }

    onValidationFailure(error){
        error.validation.errors.forEach(error => {
            console.error("Validation error in " + error.schema + "; " + error.message);
        });
        console.error("Message Json: " +  JSON.stringify(error.json));
    }
}

module.exports = {
   AuraManager
}