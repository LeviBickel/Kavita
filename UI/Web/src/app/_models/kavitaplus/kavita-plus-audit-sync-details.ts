import {ScrobbleProvider} from "../../_services/scrobbling.service";

export interface KavitaPlusAuditSyncDetails {
  // CollectionSynced
  collectionName: string | null;
  stackId: string | null;
  itemCount: number | null;
  missingCount: number | null;

  // CollectionItemAdded
  seriesName: string | null;
  seriesId: number | null;

  // SyncCompleted (WantToRead)
  userName: string | null;
  providers: ScrobbleProvider[] | null;
  seriesMatched: number | null;
}
