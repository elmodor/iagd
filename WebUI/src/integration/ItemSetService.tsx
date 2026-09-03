import { getItemSetAssociations } from './integration';

interface ItemSetAssociation {
  baseRecord: string;
  setName: string;
}


const setNameByRecord = new Map<string, string>();
const recordsBySetName = new Map<string, string[]>();
let initialized = false;

export async function initializeItemSetAssociations(): Promise<void> {
  if (initialized) {
    return;
  }

  const data = await getItemSetAssociations();
  const dataset = JSON.parse(data) as ItemSetAssociation[];
  for (const entry of dataset) {
    // First one wins, matching the previous filter(..)[0] lookup.
    if (!setNameByRecord.has(entry.baseRecord)) {
      setNameByRecord.set(entry.baseRecord, entry.setName);
    }

    const members = recordsBySetName.get(entry.setName);
    if (members) {
      members.push(entry.baseRecord);
    } else {
      recordsBySetName.set(entry.setName, [entry.baseRecord]);
    }
  }

  initialized = true;
}

// Returns the set name or undefined
export default function GetSetName(baseRecord: string): string | undefined {
  return setNameByRecord.get(baseRecord);
}

// Returns the items in a given set, or an empty list
export function GetSetItems(setName: string | undefined): string[] {
  if (setName === undefined) {
    return [];
  }

  return recordsBySetName.get(setName) ?? [];
}
