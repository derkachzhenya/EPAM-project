import type { GetParticipantsResponse, GetRoomResponse } from "@types/api";

export interface RoomPageContentProps {
  participants: GetParticipantsResponse;
  roomDetails: GetRoomResponse;
  onDrawNames: () => void;
  onDeleteParticipant?: (participantId: number) => void;
}
