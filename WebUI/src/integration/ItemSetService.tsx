import { getItemSetAssociations } from './integration';

interface ItemSetAssociation {
  baseRecord: string;
  setName: string;
}


let dataset = [] as Array<ItemSetAssociation>;
const reverseLookup: { [index: string]: string[] } = {};
let initialized = false;

export async function initializeItemSetAssociations(): Promise<void> {
  if (initialized) {
    return;
  }
  console.debug("Fetching item set associations");
  const data = await getItemSetAssociations();
  dataset = JSON.parse(data);
  for (const entry of dataset) {
    if (reverseLookup.hasOwnProperty(entry.setName)) {
      reverseLookup[entry.setName] = reverseLookup[entry.setName].concat(entry.baseRecord);
    } else {
      reverseLookup[entry.setName] = [entry.baseRecord];
    }
  }
  initialized = true;
}

// Returns the set name or undefined
export default function GetSetName(baseRecord: string): string | undefined {
  const elems = dataset.filter(elem => elem.baseRecord === baseRecord);
  if (elems.length > 0) {
    return elems[0].setName;
  }

  return undefined;
}

// Returns the items in a given set or undefined
export function GetSetItems(setName: string|undefined): string[] {
  if (setName !== undefined) {
    return reverseLookup[setName];
  }

  return [];
}
